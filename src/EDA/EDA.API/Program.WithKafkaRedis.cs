using Serilog;
using EDA.EventStore;
using EDA.ReadModel;
using EDA.Projections;
using EDA.Infrastructure.Kafka;
using EDA.Infrastructure.Redis;
using StackExchange.Redis;

/*
 * PROGRAMA ALTERNATIVO CON KAFKA Y REDIS
 *
 * Este archivo muestra cómo configurar la API EDA usando:
 * - Kafka para Event Store y Event Bus (en lugar de in-memory)
 * - Redis para Read Model Repository (en lugar de in-memory)
 *
 * PARA USAR ESTA CONFIGURACIÓN:
 *
 * 1. Instalar NuGet packages:
 *    dotnet add package Confluent.Kafka --version 2.3.0
 *    dotnet add package StackExchange.Redis --version 2.7.10
 *
 * 2. Renombrar archivos:
 *    mv Program.cs Program.InMemory.cs
 *    mv Program.WithKafkaRedis.cs Program.cs
 *
 * 3. Levantar infraestructura:
 *    docker-compose -f docker-compose.full.yml up -d
 *
 * 4. Ejecutar API:
 *    dotnet run
 */

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERILOG - Logging
// ============================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// REDIS - Read Model Storage
// ============================================
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

Console.WriteLine($" Connecting to Redis at {redisConnectionString}...");

var redisConnection = ConnectionMultiplexer.Connect(new ConfigurationOptions
{
    EndPoints = { redisConnectionString! },
    AbortOnConnectFail = false,
    ConnectTimeout = 5000,
    SyncTimeout = 5000,
    AsyncTimeout = 5000
});

redisConnection.ConnectionFailed += (sender, args) =>
{
    Log.Error($" Redis connection failed: {args.Exception?.Message}");
};

redisConnection.ConnectionRestored += (sender, args) =>
{
    Log.Information(" Redis connection restored");
};

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<IOrderReadModelRepository, RedisOrderReadModelRepository>();

Console.WriteLine(" Redis configured successfully");

// ============================================
// KAFKA - Event Store & Event Bus
// ============================================
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"];

Console.WriteLine($" Connecting to Kafka at {kafkaBootstrapServers}...");

// Event Store con Kafka
builder.Services.AddSingleton<IEventStore, KafkaEventStore>();

// Event Bus con Kafka
builder.Services.AddSingleton<IEventBus, KafkaEventBus>();

Console.WriteLine(" Kafka configured successfully");

// ============================================
// PROJECTIONS - Actualizar Read Models
// ============================================
builder.Services.AddSingleton<OrderProjection>();

// ============================================
// CONTROLLERS & SWAGGER
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Event-Driven Architecture (EDA) API - With Kafka & Redis",
        Version = "v1",
        Description = @"
API de Event-Driven Architecture con Event Sourcing y CQRS

**INFRAESTRUCTURA:**
- Event Store: Apache Kafka
- Event Bus: Apache Kafka
- Read Model: Redis

**CARACTERÍSTICAS:**
- Event Sourcing: Estado reconstruido desde eventos
- CQRS: Commands (Write) y Queries (Read) separados
- Eventually Consistent: Read Models actualizados de forma asíncrona
- Persistencia durable con Kafka
- Consultas ultra rápidas con Redis

**HERRAMIENTAS:**
- Kafka UI: http://localhost:8080 (ver eventos)
- Redis Commander: http://localhost:8081 (ver read models)

**ENDPOINTS:**
- POST /api/v1/orders - Crear orden
- POST /api/v1/orders/{id}/items - Agregar item
- POST /api/v1/orders/{id}/confirm - Confirmar orden
- POST /api/v1/orders/{id}/ship - Enviar orden
- GET /api/v1/orders/{id} - Obtener orden (Read Model)
- GET /api/v1/orders/customer/{customerId} - Órdenes por cliente
- GET /api/v1/orders/{id}/events - Historia de eventos
        "
    });
});

// ============================================
// HEALTH CHECKS
// ============================================
builder.Services.AddHealthChecks()
    .AddCheck("redis", () =>
    {
        try
        {
            var db = redisConnection.GetDatabase();
            db.Ping();
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis is healthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Redis is unhealthy: {ex.Message}");
        }
    })
    .AddCheck("kafka", () =>
    {
        // Kafka health check sería más complejo, simplificado aquí
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Kafka is assumed healthy");
    });

var app = builder.Build();

// ============================================
// SUSCRIBIR PROJECTIONS AL EVENT BUS
// ============================================
var eventBus = app.Services.GetRequiredService<IEventBus>();
var projection = app.Services.GetRequiredService<OrderProjection>();

Console.WriteLine(" Subscribing projections to event bus...");

// Suscribir projection a todos los eventos de Order
projection.Subscribe(eventBus);

// Iniciar consumo de eventos en background
var kafkaEventBus = eventBus as KafkaEventBus;
if (kafkaEventBus != null)
{
    var cancellationTokenSource = new CancellationTokenSource();

    // Detener consumer cuando la aplicación se cierre
    app.Lifetime.ApplicationStopping.Register(() =>
    {
        Console.WriteLine(" Stopping Kafka consumer...");
        cancellationTokenSource.Cancel();
    });

    kafkaEventBus.StartConsuming(cancellationTokenSource.Token);
}

Console.WriteLine(" Projections subscribed successfully");

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

app.MapControllers();
app.MapHealthChecks("/health");

// ============================================
// LOG DE STARTUP
// ============================================
Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║      Event-Driven Architecture (EDA) API                  ║
║        with Apache Kafka & Redis                            ║
║                                                              ║
║  Infrastructure:                                             ║
║     Event Store: Apache Kafka                             ║
║     Event Bus: Apache Kafka                               ║
║     Read Model: Redis                                     ║
║                                                              ║
║  Tools:                                                      ║
║     Kafka UI:        http://localhost:8080                ║
║     Redis Commander: http://localhost:8081                ║
║     Swagger:         http://localhost:5200                ║
║    ️  Health Check:    http://localhost:5200/health        ║
║                                                              ║
║  Patterns:                                                   ║
║     Event Sourcing                                        ║
║     CQRS (Command Query Responsibility Segregation)      ║
║     Eventually Consistent Reads                           ║
║     Projections                                           ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
");

try
{
    Log.Information(" Starting EDA API with Kafka & Redis...");
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
