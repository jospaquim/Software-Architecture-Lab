using System.Text;
using System.Threading.RateLimiting;
using DDD.Sales.API.Middleware;
using DDD.Sales.Application.Commands.CreateOrder;
using DDD.Sales.Application.Queries.GetOrder;
using DDD.Sales.Infrastructure.Persistence;
using DDD.Sales.Infrastructure.Repositories;
using DDD.Sales.Infrastructure.Resilience;
using DDD.Sales.Infrastructure.ExternalServices;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using FluentValidation;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERILOG - Logging
// ============================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/ddd-sales-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// OPENTELEMETRY - Observability
// ============================================
var serviceName = "DDD.Sales.API";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName,
            ["architecture.pattern"] = "DDD"
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("http.request.client_ip", httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString());
            };
            options.EnrichWithHttpResponse = (activity, httpResponse) =>
            {
                activity.SetTag("http.response.status_code", httpResponse.StatusCode);
            };
        })
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation(options =>
        {
            options.RecordException = true;
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
        })
        .AddOtlpExporter(options =>
        {
            // Jaeger endpoint (OTLP)
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                ?? "http://localhost:4317");
        }));

// ============================================
// CONTROLLERS
// ============================================
builder.Services.AddControllers();

// ============================================
// MEDIATR - CQRS
// ============================================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly);
});

// ============================================
// AUTOMAPPER - Object Mapping
// ============================================
builder.Services.AddAutoMapper(typeof(CreateOrderCommand).Assembly);

// ============================================
// FLUENTVALIDATION - Validation
// ============================================
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderCommand).Assembly);

// ============================================
// DATABASE - EF CORE
// ============================================
builder.Services.AddDbContext<SalesDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// ============================================
// REPOSITORIES
// ============================================
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ============================================
// REDIS - Distributed Cache
// ============================================
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "DDDSales:";
});
builder.Services.AddScoped<DDD.Sales.Infrastructure.Caching.ICacheService,
    DDD.Sales.Infrastructure.Caching.RedisCacheService>();

// ============================================
// POLLY - Resilience Patterns
// ============================================
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentGateway:BaseUrl"] ?? "https://api.payment-gateway.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler((services, request) =>
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    return ResiliencePolicies.GetCombinedPolicy(logger);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// ============================================
// AUTHENTICATION - JWT
// ============================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ??
                throw new InvalidOperationException("JWT SecretKey not configured")))
    };
});

builder.Services.AddAuthorization();

// ============================================
// RATE LIMITING
// ============================================
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter - 100 requests por minuto
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });

    // Sliding window limiter - Más sofisticado
    options.AddSlidingWindowLimiter("sliding", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
        opt.SegmentsPerWindow = 4;
    });

    // Concurrency limiter - Máximo 50 requests concurrentes
    options.AddConcurrencyLimiter("concurrency", opt =>
    {
        opt.PermitLimit = 50;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    // Token bucket limiter - Para burst traffic
    options.AddTokenBucketLimiter("token", opt =>
    {
        opt.TokenLimit = 100;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ============================================
// CORS
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",  // Angular
            "http://localhost:3000"   // Next.js
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// ============================================
// HEALTH CHECKS
// ============================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SalesDbContext>(
        name: "database",
        tags: new[] { "db", "sql" })
    .AddCheck("self", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"))
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var threshold = 1024L * 1024L * 1024L; // 1 GB

        return allocated < threshold
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"Memory usage: {allocated / 1024 / 1024} MB")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                $"Memory usage high: {allocated / 1024 / 1024} MB");
    });

// ============================================
// SWAGGER / OPENAPI
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DDD Sales API - Domain-Driven Design",
        Version = "v1",
        Description = @"
API de Sales Bounded Context usando Domain-Driven Design (DDD)

**CARACTERÍSTICAS:**
- Value Objects (Money, Email, Address)
- Aggregates con lógica de negocio encapsulada
- Strongly Typed IDs
- Domain Services (PricingService)
- Specification Pattern
- Domain Events
- Repository Pattern
- CQRS con MediatR

**TACTICAL PATTERNS:**
- Entities, Value Objects, Aggregates
- Domain Services
- Repositories
- Factories
- Specifications

**STRATEGIC PATTERNS:**
- Bounded Context (Sales)
- Ubiquitous Language
- Context Mapping (ready for integration)

**ENDPOINTS:**
- POST /api/v1/orders - Crear orden
- POST /api/v1/orders/{id}/items - Agregar item
- GET /api/v1/orders/{id} - Obtener orden
        ",
        Contact = new OpenApiContact
        {
            Name = "DDD Architecture Team",
            Email = "ddd@example.com"
        }
    });

    // JWT Authentication en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ============================================
// MIDDLEWARE PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DDD Sales API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

app.UseSerilogRequestLogging();

app.UseSecurityHeaders();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireRateLimiting("sliding"); // Rate limiting global

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Solo verifica que la app esté corriendo
});

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// ============================================
// RUN
// ============================================
try
{
    Log.Information(" Starting DDD Sales API...");
    Log.Information(" Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information(" Authentication: JWT Bearer");
    Log.Information(" Rate Limiting: Enabled (100 req/min)");
    Log.Information("️  Security Headers: Enabled (11 headers)");
    Log.Information("️  Health Checks: /health, /health/ready, /health/live");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
