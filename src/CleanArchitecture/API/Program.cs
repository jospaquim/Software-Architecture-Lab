using System.Text;
using System.Threading.RateLimiting;
using CleanArchitecture.API.Middleware;
using CleanArchitecture.Application.Mapping;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Resilience;
using CleanArchitecture.Infrastructure.ExternalServices;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
var serviceName = "CleanArchitecture.API";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
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

// Add services to the container
builder.Services.AddControllers();

// ============================================
// FEATURE FLAGS
// ============================================
builder.Services.AddFeatureManagement();

// ============================================
// PERFORMANCE OPTIMIZATIONS
// ============================================
// Response Compression (Gzip + Brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Output Caching (.NET 8)
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
    options.AddPolicy("Cache5Min", builder => builder.Expire(TimeSpan.FromMinutes(5)));
});

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CleanArchitecture.Application.Common.Result).Assembly);
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CleanArchitecture.Application.Common.Result).Assembly);

// Add Infrastructure services (DbContext, Repositories, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================
// REDIS - Distributed Cache
// ============================================
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "CleanArchitecture:";
});
builder.Services.AddScoped<CleanArchitecture.Infrastructure.Caching.ICacheService,
    CleanArchitecture.Infrastructure.Caching.RedisCacheService>();

// ============================================
// POLLY - Resilience Patterns (HttpClient)
// ============================================
// Configure HttpClient with Polly resilience policies
builder.Services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApi:BaseUrl"] ?? "https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "CleanArchitecture-API/1.0");
})
.AddPolicyHandler((services, request) =>
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    return ResiliencePolicies.GetCombinedPolicy(logger);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Prevent port exhaustion

// Alternative: Named HttpClient with specific policies
builder.Services.AddHttpClient("ResilientClient")
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        // Use retry policy with jitter for GET requests
        return request.Method == HttpMethod.Get
            ? ResiliencePolicies.GetRetryPolicyWithJitter(logger)
            : Policy.NoOpAsync<HttpResponseMessage>();
    })
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        return ResiliencePolicies.GetCircuitBreakerPolicy(logger);
    })
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        return ResiliencePolicies.GetTimeoutPolicy(logger);
    });

// Add Authentication - JWT
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured")))
    };
});

builder.Services.AddAuthorization();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });

    // Sliding window limiter (more sophisticated)
    options.AddSlidingWindowLimiter("sliding", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
        opt.SegmentsPerWindow = 4;
    });

    // Concurrency limiter
    options.AddConcurrencyLimiter("concurrency", opt =>
    {
        opt.PermitLimit = 50;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

    options.AddPolicy("Production",
        policy =>
        {
            policy.WithOrigins(
                    "https://yourdomain.com",
                    "https://www.yourdomain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CleanArchitecture.Infrastructure.Persistence.ApplicationDbContext>(
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

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clean Architecture API",
        Version = "v1",
        Description = "A RESTful API built with Clean Architecture principles",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com",
            Url = new Uri("https://github.com/yourusername")
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Include XML comments (optional)
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}
else
{
    // Production Swagger (optional - remove in production if not needed)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API v1");
    });
}

// Global Exception Handler Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// HTTPS Redirection
app.UseHttpsRedirection();

// Response Compression
app.UseResponseCompression();

// Output Caching
app.UseOutputCache();

// CORS
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

// Rate Limiting
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Map Controllers
app.MapControllers();

// Request logging
app.UseSerilogRequestLogging();

try
{
    Log.Information("Starting Clean Architecture API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
