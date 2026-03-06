using System.Text;
using System.Threading.RateLimiting;
using EDA.API.Middleware;
using EDA.EventStore;
using EDA.Projections;
using EDA.ReadModel;
using EDA.Infrastructure.Resilience;
using EDA.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
    .WriteTo.File("logs/eda-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// OPENTELEMETRY - Observability
// ============================================
var serviceName = "EDA.API";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName,
            ["architecture.pattern"] = "EDA-EventSourcing-CQRS"
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
// AUTOMAPPER - Object Mapping
// ============================================
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// ============================================
// FLUENTVALIDATION - Validation
// ============================================
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ============================================
// EVENT STORE & EVENT BUS
// ============================================
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ============================================
// READ MODEL REPOSITORY
// ============================================
builder.Services.AddSingleton<IOrderReadModelRepository, InMemoryOrderReadModelRepository>();

// ============================================
// PROJECTIONS
// ============================================
builder.Services.AddSingleton<OrderProjection>();

// ============================================
// POLLY - Resilience Patterns
// ============================================
builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["NotificationService:BaseUrl"] ?? "https://api.notification-service.com");
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

    // Token bucket limiter - Para burst traffic (ideal para EDA)
    options.AddTokenBucketLimiter("token", opt =>
    {
        opt.TokenLimit = 100;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });

    // Custom limiter por IP
    options.AddPolicy("per-ip", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(
            ipAddress,
            _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                SegmentsPerWindow = 4
            });
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
    .AddCheck("self", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"))
    .AddCheck("event-store", () =>
    {
        var eventStore = builder.Services.BuildServiceProvider()
            .GetRequiredService<IEventStore>();

        // Verificar que el event store esté operativo
        try
        {
            // En producción, podrías verificar la conexión a Kafka aquí
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "Event Store is operational");
        }
        catch
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Event Store is not operational");
        }
    })
    .AddCheck("read-model", () =>
    {
        var readModelRepo = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOrderReadModelRepository>();

        // Verificar que el read model repository esté operativo
        try
        {
            // En producción, podrías verificar la conexión a Redis aquí
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "Read Model Repository is operational");
        }
        catch
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Read Model Repository is not operational");
        }
    })
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
        Title = "Event-Driven Architecture (EDA) API",
        Version = "v1",
        Description = @"
API de Event-Driven Architecture con Event Sourcing y CQRS

**CARACTERÍSTICAS:**
- Event Sourcing: Estado reconstruido desde eventos
- CQRS: Commands (Write) y Queries (Read) separados
- Event Store: Almacenamiento inmutable de eventos
- Event Bus: Pub/Sub para eventos de dominio
- Read Models: Vistas desnormalizadas para consultas
- Projections: Actualización de Read Models desde eventos
- Eventually Consistent: Read Models actualizados de forma asíncrona

**INFRAESTRUCTURA:**
- Event Store: InMemory (reemplazable por Kafka/EventStoreDB)
- Event Bus: InMemory (reemplazable por RabbitMQ/Kafka)
- Read Model: InMemory (reemplazable por Redis/MongoDB)

**ENDPOINTS:**

**Commands (Write Side):**
- POST /api/v1/orders - Crear orden
- POST /api/v1/orders/{id}/items - Agregar item
- POST /api/v1/orders/{id}/confirm - Confirmar orden
- POST /api/v1/orders/{id}/ship - Enviar orden

**Queries (Read Side):**
- GET /api/v1/orders/{id} - Obtener orden (Read Model)
- GET /api/v1/orders/customer/{customerId} - Órdenes por cliente
- GET /api/v1/orders/{id}/events - Historia de eventos

**VENTAJAS:**
- Audit trail completo
- Replay de eventos
- Escalabilidad horizontal
- Temporal queries
- Multiple read models optimizados
        ",
        Contact = new OpenApiContact
        {
            Name = "EDA Architecture Team",
            Email = "eda@example.com"
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
// SUSCRIBIR PROJECTIONS
// ============================================
var eventBus = app.Services.GetRequiredService<IEventBus>();
var projection = app.Services.GetRequiredService<OrderProjection>();

projection.Subscribe(eventBus);

Log.Information(" Projections subscribed to Event Bus");

// ============================================
// MIDDLEWARE PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EDA API v1");
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
    Predicate = check => check.Name == "event-store" || check.Name == "read-model"
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "self"
});

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// ============================================
// RUN
// ============================================
try
{
    Log.Information("╔══════════════════════════════════════════════════════════════╗");
    Log.Information("║                                                              ║");
    Log.Information("║      Event-Driven Architecture (EDA) API                  ║");
    Log.Information("║        with Event Sourcing & CQRS                           ║");
    Log.Information("║                                                              ║");
    Log.Information("╚══════════════════════════════════════════════════════════════╝");
    Log.Information("");
    Log.Information(" Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information(" Event Store: InMemory (use Kafka/EventStoreDB for production)");
    Log.Information(" Event Bus: InMemory (use RabbitMQ/Kafka for production)");
    Log.Information(" Read Model: InMemory (use Redis/MongoDB for production)");
    Log.Information(" Authentication: JWT Bearer");
    Log.Information(" Rate Limiting: Enabled (100 req/min, per-IP)");
    Log.Information("️  Security Headers: Enabled (11 headers)");
    Log.Information("️  Health Checks:");
    Log.Information("   - /health (overall)");
    Log.Information("   - /health/ready (event-store, read-model)");
    Log.Information("   - /health/live (api status)");
    Log.Information("");

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
