#  Guía de Redis y Kafka para Event-Driven Architecture (EDA)

##  Tabla de Contenidos
1. [¿Qué son Redis y Kafka?](#qué-son-redis-y-kafka)
2. [¿Cuándo usar cada uno?](#cuándo-usar-cada-uno)
3. [Instalación local (Para desarrollo)](#instalación-local)
4. [Implementación con Redis](#implementación-con-redis)
5. [Implementación con Kafka](#implementación-con-kafka)
6. [Docker Compose completo](#docker-compose-completo)
7. [Troubleshooting común](#troubleshooting-común)

---

##  ¿Qué son Redis y Kafka?

### Redis (Read Model Storage)
**Redis** es una base de datos **en memoria** ultra rápida que funciona como un diccionario clave-valor.

**Analogía simple:**
Imagina un archivero donde cada carpeta tiene una etiqueta (clave) y dentro hay documentos (valor). Redis puede buscar cualquier carpeta instantáneamente porque todo está en la memoria RAM.

**¿Para qué se usa en EDA?**
- Almacenar **Read Models** (vistas desnormalizadas)
- Cache de consultas frecuentes
- Lecturas ultra rápidas (microsegundos)

**Ventajas:**
-  Velocidad extrema (100,000+ operaciones/segundo)
-  Estructuras de datos avanzadas (listas, sets, hashes)
-  Persistencia opcional en disco
-  Replicación master-slave

**Desventajas:**
-  Limitado por la RAM disponible
-  Consultas complejas limitadas

---

### Kafka (Event Store & Event Bus)
**Kafka** es un sistema de **mensajería distribuido** que almacena eventos en orden.

**Analogía simple:**
Imagina un libro de contabilidad donde cada transacción se escribe en orden y nunca se borra. Múltiples personas pueden leer el libro desde cualquier página, pero nadie puede modificar lo escrito.

**¿Para qué se usa en EDA?**
- Almacenar **eventos** (Event Store)
- Publicar/suscribirse a eventos (Event Bus)
- Event Sourcing persistente

**Ventajas:**
-  Eventos inmutables y ordenados
- ️ Retención de eventos (días, semanas, para siempre)
-  Alto throughput (millones de eventos/segundo)
-  Replay de eventos desde cualquier punto
-  Múltiples consumidores independientes

**Desventajas:**
-  Configuración más compleja
-  Requiere Zookeeper (o KRaft en versiones nuevas)
-  Curva de aprendizaje más alta

---

##  ¿Cuándo usar cada uno?

| Necesidad | Tecnología | Por qué |
|-----------|------------|---------|
| Almacenar eventos permanentemente | **Kafka** | Retención infinita, replay |
| Leer datos frecuentemente | **Redis** | Ultra rápido, en memoria |
| Comunicar microservicios con eventos | **Kafka** | Pub/Sub confiable |
| Cache de consultas | **Redis** | Reduce carga en DB principal |
| Event Sourcing | **Kafka** | Diseñado para streams de eventos |
| Session storage | **Redis** | Rápido acceso por clave |

**En nuestra arquitectura EDA:**
- **Kafka** → Event Store (Write Side) + Event Bus
- **Redis** → Read Model Repository (Query Side)

---

##  Instalación Local (Para desarrollo)

### Opción 1: Docker (Recomendado para principiantes)
No necesitas instalar nada excepto Docker. Todo se levanta con `docker-compose`.

```bash
# Levantar Redis y Kafka
cd src/EDA
docker-compose up -d

# Ver logs
docker-compose logs -f
```

### Opción 2: Instalación manual

#### Redis en Windows
```bash
# Usar Chocolatey
choco install redis-64

# O descargar desde:
# https://github.com/microsoftarchive/redis/releases
```

#### Redis en Linux/Mac
```bash
# Ubuntu/Debian
sudo apt-get install redis-server
sudo systemctl start redis

# Mac
brew install redis
brew services start redis

# Verificar
redis-cli ping
# Debería responder: PONG
```

#### Kafka en Windows/Linux/Mac
**¡NO LO HAGAS MANUALMENTE!** Kafka es complejo de configurar. Usa Docker.

Si insistes:
```bash
# Descargar de https://kafka.apache.org/downloads
# Descomprimir

# 1. Iniciar Zookeeper
bin/zookeeper-server-start.sh config/zookeeper.properties

# 2. En otra terminal, iniciar Kafka
bin/kafka-server-start.sh config/server.properties
```

---

##  Implementación con Redis

### Paso 1: Instalar NuGet packages

```bash
cd src/EDA/EDA.API
dotnet add package StackExchange.Redis --version 2.7.10
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.0
```

### Paso 2: Configuración en appsettings.json

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "EDA:"
  }
}
```

### Paso 3: Crear implementación de Read Model Repository

**Archivo: `/src/EDA/Infrastructure/Redis/RedisOrderReadModelRepository.cs`**

```csharp
using StackExchange.Redis;
using System.Text.Json;
using EDA.ReadModel;

namespace EDA.Infrastructure.Redis;

/// <summary>
/// Implementación de Read Model usando Redis
/// Redis almacena datos en memoria para consultas ultra rápidas
/// </summary>
public class RedisOrderReadModelRepository : IOrderReadModelRepository
{
    private readonly IDatabase _redis;
    private const string OrderPrefix = "order:";
    private const string CustomerOrdersPrefix = "customer:orders:";

    public RedisOrderReadModelRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task<OrderReadModel?> GetByIdAsync(Guid orderId)
    {
        var key = $"{OrderPrefix}{orderId}";
        var json = await _redis.StringGetAsync(key);

        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<OrderReadModel>(json!);
    }

    public async Task<IEnumerable<OrderReadModel>> GetByCustomerAsync(Guid customerId)
    {
        // Obtener lista de IDs de órdenes del cliente
        var key = $"{CustomerOrdersPrefix}{customerId}";
        var orderIds = await _redis.SetMembersAsync(key);

        if (orderIds.Length == 0)
            return Enumerable.Empty<OrderReadModel>();

        // Obtener todas las órdenes en paralelo
        var tasks = orderIds.Select(id => GetByIdAsync(Guid.Parse(id!)));
        var orders = await Task.WhenAll(tasks);

        return orders.Where(o => o != null)!;
    }

    public async Task<IEnumerable<OrderReadModel>> GetByStatusAsync(string status)
    {
        // Para búsquedas por status, usamos un Set en Redis
        var key = $"status:orders:{status}";
        var orderIds = await _redis.SetMembersAsync(key);

        var tasks = orderIds.Select(id => GetByIdAsync(Guid.Parse(id!)));
        var orders = await Task.WhenAll(tasks);

        return orders.Where(o => o != null)!;
    }

    public async Task<IEnumerable<OrderReadModel>> GetAllAsync(int skip, int take)
    {
        // Nota: Redis no es ideal para paginación de todos los registros
        // Esta es una implementación simple. Para producción, considera usar Redis Sorted Sets

        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{OrderPrefix}*")
                        .Skip(skip)
                        .Take(take)
                        .ToList();

        var tasks = keys.Select(key => _redis.StringGetAsync(key));
        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => !r.IsNullOrEmpty)
            .Select(r => JsonSerializer.Deserialize<OrderReadModel>(r!))
            .Where(o => o != null)!;
    }

    public async Task SaveAsync(OrderReadModel model)
    {
        var key = $"{OrderPrefix}{model.Id}";
        var json = JsonSerializer.Serialize(model);

        // Guardar la orden
        await _redis.StringSetAsync(key, json);

        // Agregar a índices secundarios
        await _redis.SetAddAsync($"{CustomerOrdersPrefix}{model.CustomerId}", model.Id.ToString());
        await _redis.SetAddAsync($"status:orders:{model.Status}", model.Id.ToString());

        // Opcional: TTL (Time To Live) - expira en 30 días
        await _redis.KeyExpireAsync(key, TimeSpan.FromDays(30));
    }

    public async Task UpdateAsync(OrderReadModel model)
    {
        // En Redis, Update es igual que Save
        await SaveAsync(model);
    }

    public async Task DeleteAsync(Guid orderId)
    {
        // Primero obtenemos la orden para limpiar índices
        var order = await GetByIdAsync(orderId);
        if (order == null) return;

        var key = $"{OrderPrefix}{orderId}";

        // Eliminar de índices
        await _redis.SetRemoveAsync($"{CustomerOrdersPrefix}{order.CustomerId}", orderId.ToString());
        await _redis.SetRemoveAsync($"status:orders:{order.Status}", orderId.ToString());

        // Eliminar la orden
        await _redis.KeyDeleteAsync(key);
    }
}
```

### Paso 4: Registrar en Program.cs

```csharp
// Program.cs
using StackExchange.Redis;
using EDA.Infrastructure.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configurar Redis
var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString");
var redis = ConnectionMultiplexer.Connect(redisConnection!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Registrar repositorio Redis
builder.Services.AddSingleton<IOrderReadModelRepository, RedisOrderReadModelRepository>();

// ... resto de configuración
```

---

##  Implementación con Kafka

### Paso 1: Instalar NuGet packages

```bash
cd src/EDA/EDA.API
dotnet add package Confluent.Kafka --version 2.3.0
```

### Paso 2: Configuración en appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "eda-order-service",
    "Topics": {
      "Orders": "orders-events"
    }
  }
}
```

### Paso 3: Crear EventStore con Kafka

**Archivo: `/src/EDA/Infrastructure/Kafka/KafkaEventStore.cs`**

```csharp
using Confluent.Kafka;
using System.Text.Json;
using EDA.EventStore;

namespace EDA.Infrastructure.Kafka;

/// <summary>
/// Event Store usando Kafka
/// Los eventos se almacenan como mensajes en topics de Kafka
/// Kafka garantiza orden y persistencia de eventos
/// </summary>
public class KafkaEventStore : IEventStore
{
    private readonly IProducer<string, string> _producer;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _topic;

    public KafkaEventStore(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            // Configuración para garantizar durabilidad
            Acks = Acks.All, // Esperar confirmación de todos los replicas
            EnableIdempotence = true, // Prevenir duplicados
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest, // Leer desde el principio
            EnableAutoCommit = false // Control manual de offsets
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _topic = configuration["Kafka:Topics:Orders"] ?? "orders-events";
    }

    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, int expectedVersion)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any()) return;

        // En Kafka, el aggregateId es la partition key
        // Esto garantiza que todos los eventos de un aggregate van a la misma partición
        // manteniendo el orden
        var key = aggregateId.ToString();

        foreach (var @event in eventsList)
        {
            var eventWrapper = new KafkaEventWrapper
            {
                EventId = @event.EventId,
                AggregateId = @event.AggregateId,
                Version = @event.Version,
                OccurredAt = @event.OccurredAt,
                EventType = @event.EventType,
                EventData = JsonSerializer.Serialize(@event, @event.GetType()),
                Metadata = JsonSerializer.Serialize(new
                {
                    ClrType = @event.GetType().AssemblyQualifiedName,
                    SavedAt = DateTime.UtcNow,
                    ExpectedVersion = expectedVersion
                })
            };

            var json = JsonSerializer.Serialize(eventWrapper);

            // Publicar a Kafka
            var message = new Message<string, string>
            {
                Key = key,
                Value = json,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.EventType) },
                    { "aggregate-id", System.Text.Encoding.UTF8.GetBytes(aggregateId.ToString()) }
                }
            };

            var result = await _producer.ProduceAsync(_topic, message);

            // Log del resultado
            Console.WriteLine($"Event {eventWrapper.EventId} delivered to partition {result.Partition} at offset {result.Offset}");
        }
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId)
    {
        return await GetEventsAsync(aggregateId, fromVersion: 1);
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion)
    {
        var events = new List<IEvent>();
        var key = aggregateId.ToString();

        // Suscribirse al topic
        _consumer.Subscribe(_topic);

        try
        {
            // Leer eventos del topic
            // Nota: En producción, considera usar Kafka Streams o un consumer dedicado
            var timeout = TimeSpan.FromSeconds(5);
            var endTime = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < endTime)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));

                if (consumeResult == null) continue;

                // Filtrar solo eventos del aggregate que buscamos
                if (consumeResult.Message.Key != key) continue;

                var wrapper = JsonSerializer.Deserialize<KafkaEventWrapper>(consumeResult.Message.Value);
                if (wrapper == null || wrapper.Version < fromVersion) continue;

                var @event = DeserializeEvent(wrapper);
                events.Add(@event);
            }

            _consumer.Commit();
        }
        finally
        {
            _consumer.Unsubscribe();
        }

        return events.OrderBy(e => e.Version);
    }

    public async Task<IEnumerable<IEvent>> GetAllEventsAsync(DateTime? from = null, DateTime? to = null)
    {
        var events = new List<IEvent>();

        _consumer.Subscribe(_topic);

        try
        {
            var timeout = TimeSpan.FromSeconds(10);
            var endTime = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < endTime)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult == null) continue;

                var wrapper = JsonSerializer.Deserialize<KafkaEventWrapper>(consumeResult.Message.Value);
                if (wrapper == null) continue;

                // Filtrar por rango de fechas
                if (from.HasValue && wrapper.OccurredAt < from.Value) continue;
                if (to.HasValue && wrapper.OccurredAt > to.Value) continue;

                var @event = DeserializeEvent(wrapper);
                events.Add(@event);
            }

            _consumer.Commit();
        }
        finally
        {
            _consumer.Unsubscribe();
        }

        return events.OrderBy(e => e.OccurredAt);
    }

    private IEvent DeserializeEvent(KafkaEventWrapper wrapper)
    {
        var metadata = JsonSerializer.Deserialize<EventMetadata>(wrapper.Metadata ?? "{}");
        var eventType = Type.GetType(metadata?.ClrType ?? wrapper.EventType);

        if (eventType == null)
            throw new InvalidOperationException($"Cannot deserialize event type: {wrapper.EventType}");

        var @event = JsonSerializer.Deserialize(wrapper.EventData, eventType) as IEvent;

        if (@event == null)
            throw new InvalidOperationException($"Failed to deserialize event: {wrapper.EventId}");

        return @event;
    }

    public void Dispose()
    {
        _producer?.Dispose();
        _consumer?.Dispose();
    }

    private class EventMetadata
    {
        public string? ClrType { get; set; }
        public DateTime SavedAt { get; set; }
        public int ExpectedVersion { get; set; }
    }
}

public class KafkaEventWrapper
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public int Version { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}
```

### Paso 4: Crear EventBus con Kafka

**Archivo: `/src/EDA/Infrastructure/Kafka/KafkaEventBus.cs`**

```csharp
using Confluent.Kafka;
using System.Text.Json;
using EDA.EventStore;

namespace EDA.Infrastructure.Kafka;

/// <summary>
/// Event Bus usando Kafka para Pub/Sub
/// Publica eventos que múltiples servicios pueden consumir
/// </summary>
public class KafkaEventBus : IEventBus
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly Dictionary<Type, List<Func<IEvent, Task>>> _handlers = new();

    public KafkaEventBus(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topic = configuration["Kafka:Topics:Orders"] ?? "orders-events";
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var json = JsonSerializer.Serialize(@event);

        var message = new Message<string, string>
        {
            Key = @event.AggregateId.ToString(),
            Value = json,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.EventType) }
            }
        };

        var result = await _producer.ProduceAsync(_topic, message);
        Console.WriteLine($"Published event {@event.EventType} to partition {result.Partition}");
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);

        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Func<IEvent, Task>>();

        _handlers[eventType].Add(async (@event) => await handler((TEvent)@event));
    }

    // Método para iniciar consumer en background
    public void StartConsuming(CancellationToken cancellationToken)
    {
        Task.Run(() => ConsumeEvents(cancellationToken), cancellationToken);
    }

    private async Task ConsumeEvents(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "eda-event-bus",
            AutoOffsetReset = AutoOffsetReset.Latest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(cancellationToken);
                await ProcessMessage(result.Message);
                consumer.Commit(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error consuming event: {ex.Message}");
            }
        }
    }

    private async Task ProcessMessage(Message<string, string> message)
    {
        // Deserializar evento basado en el header
        var eventTypeHeader = message.Headers.FirstOrDefault(h => h.Key == "event-type");
        if (eventTypeHeader == null) return;

        var eventTypeName = System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());

        // Encontrar handlers registrados
        foreach (var handlerList in _handlers.Values)
        {
            foreach (var handler in handlerList)
            {
                // Aquí necesitarías deserializar al tipo correcto
                // Para simplificar, asumimos que tienes una forma de mapear el tipo
                // En producción, usa un registro de tipos o reflexión
                await Task.CompletedTask; // Placeholder
            }
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
```

### Paso 5: Registrar en Program.cs

```csharp
// Program.cs
using EDA.Infrastructure.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Registrar Kafka Event Store
builder.Services.AddSingleton<IEventStore, KafkaEventStore>();

// Registrar Kafka Event Bus
builder.Services.AddSingleton<IEventBus, KafkaEventBus>();

// ... resto de configuración
```

---

##  Docker Compose Completo

**Archivo: `/src/EDA/docker-compose.full.yml`**

```yaml
version: '3.8'

services:
  # Tu API de EDA
  eda-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: eda-api
    ports:
      - "5200:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - Redis__ConnectionString=redis:6379
      - Kafka__BootstrapServers=kafka:29092
    depends_on:
      - redis
      - kafka
    networks:
      - eda-network
    restart: unless-stopped

  # Redis para Read Models
  redis:
    image: redis:7-alpine
    container_name: eda-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    networks:
      - eda-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

  # Zookeeper (requerido por Kafka)
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    container_name: eda-zookeeper
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    networks:
      - eda-network
    restart: unless-stopped

  # Kafka para Event Store y Event Bus
  kafka:
    image: confluentinc/cp-kafka:7.5.0
    container_name: eda-kafka
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
      - "29092:29092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      # Listeners para acceso interno y externo
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      # Configuración para retención de eventos
      KAFKA_LOG_RETENTION_HOURS: 168  # 7 días
      KAFKA_LOG_RETENTION_BYTES: 1073741824  # 1GB
    volumes:
      - kafka-data:/var/lib/kafka/data
    networks:
      - eda-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "kafka-broker-api-versions", "--bootstrap-server", "localhost:9092"]
      interval: 10s
      timeout: 10s
      retries: 5

  # Kafka UI (Herramienta visual para ver topics, mensajes, etc.)
  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: eda-kafka-ui
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: local
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:29092
      KAFKA_CLUSTERS_0_ZOOKEEPER: zookeeper:2181
    networks:
      - eda-network
    restart: unless-stopped

  # Redis Commander (Herramienta visual para ver datos en Redis)
  redis-commander:
    image: rediscommander/redis-commander:latest
    container_name: eda-redis-commander
    depends_on:
      - redis
    ports:
      - "8081:8081"
    environment:
      - REDIS_HOSTS=local:redis:6379
    networks:
      - eda-network
    restart: unless-stopped

networks:
  eda-network:
    driver: bridge

volumes:
  redis-data:
  kafka-data:
```

---

##  Cómo usar (Paso a paso para juniors)

### 1. Levantar todo con Docker

```bash
cd /home/user/SoftwareArchitecture/src/EDA

# Levantar servicios
docker-compose -f docker-compose.full.yml up -d

# Ver logs en tiempo real
docker-compose -f docker-compose.full.yml logs -f

# Verificar que todos estén corriendo
docker-compose -f docker-compose.full.yml ps
```

Deberías ver:
-  eda-api (puerto 5200)
-  redis (puerto 6379)
-  kafka (puerto 9092)
-  zookeeper (puerto 2181)
-  kafka-ui (puerto 8080)
-  redis-commander (puerto 8081)

### 2. Acceder a las herramientas visuales

**Kafka UI** (Ver eventos en Kafka):
- URL: http://localhost:8080
- Aquí puedes ver:
  - Topics creados
  - Mensajes (eventos) en cada topic
  - Particiones
  - Consumer groups

**Redis Commander** (Ver datos en Redis):
- URL: http://localhost:8081
- Aquí puedes ver:
  - Todas las claves almacenadas
  - Valores en cada clave
  - TTL (tiempo de expiración)

### 3. Probar tu API

```bash
# Crear una orden
curl -X POST http://localhost:5200/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000"
  }'

# Respuesta: {"orderId": "..."}
```

### 4. Ver qué pasó

**En Kafka UI** (http://localhost:8080):
1. Click en "Topics"
2. Click en "orders-events"
3. Click en "Messages"
4. Verás el evento `OrderCreatedEvent` almacenado

**En Redis Commander** (http://localhost:8081):
1. Busca la clave `order:{orderId}`
2. Verás el Read Model JSON almacenado

### 5. Detener todo

```bash
# Detener sin borrar datos
docker-compose -f docker-compose.full.yml stop

# Detener y borrar contenedores (mantiene volúmenes)
docker-compose -f docker-compose.full.yml down

# Detener, borrar todo incluyendo datos
docker-compose -f docker-compose.full.yml down -v
```

---

##  Troubleshooting Común

### Problema 1: Kafka no arranca

**Síntoma:** `docker-compose logs kafka` muestra errores de conexión a Zookeeper

**Solución:**
```bash
# Kafka depende de Zookeeper. Espera 30 segundos después de levantar Zookeeper
docker-compose up -d zookeeper
sleep 30
docker-compose up -d kafka
```

### Problema 2: "Connection refused" desde .NET

**Síntoma:** Tu API no puede conectarse a Redis o Kafka

**Causa:** Connection strings incorrectos

**Solución:**
```json
// appsettings.Development.json
{
  "Redis": {
    "ConnectionString": "localhost:6379"  // Desde host
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"  // Desde host
  }
}

// appsettings.Docker.json (cuando tu API corre en Docker)
{
  "Redis": {
    "ConnectionString": "redis:6379"  // Desde Docker network
  },
  "Kafka": {
    "BootstrapServers": "kafka:29092"  // Desde Docker network
  }
}
```

### Problema 3: Eventos no aparecen en Kafka

**Verificar:**
```bash
# Entrar al contenedor de Kafka
docker exec -it eda-kafka bash

# Listar topics
kafka-topics --bootstrap-server localhost:9092 --list

# Ver mensajes en un topic
kafka-console-consumer --bootstrap-server localhost:9092 \
  --topic orders-events \
  --from-beginning
```

### Problema 4: Redis pierde datos al reiniciar

**Causa:** No está configurada persistencia

**Solución:** Ya está en el docker-compose con:
```yaml
command: redis-server --appendonly yes
volumes:
  - redis-data:/data
```

### Problema 5: Puertos ocupados

**Síntoma:** `port is already allocated`

**Solución:**
```bash
# Ver qué está usando el puerto
# En Windows
netstat -ano | findstr :9092

# En Linux/Mac
lsof -i :9092

# Matar proceso o cambiar puerto en docker-compose.yml
```

---

##  Diagrama de Flujo

```
┌─────────────┐
│   Cliente   │
└──────┬──────┘
       │ POST /api/v1/orders
       ▼
┌─────────────────────────────────┐
│     OrdersController (API)      │
│  1. Recibe comando              │
│  2. Crea OrderAggregate         │
└──────┬──────────────────┬───────┘
       │                  │
       │ SaveEvents       │ PublishAsync
       ▼                  ▼
┌─────────────┐    ┌──────────────┐
│   KAFKA     │    │  KAFKA       │
│ Event Store │    │  Event Bus   │
│             │    │  (mismo)     │
│ Topic:      │    │              │
│ orders-     │    │              │
│ events      │    │              │
└─────────────┘    └──────┬───────┘
                          │
                          │ Subscribe
                          ▼
                   ┌──────────────┐
                   │ Projection   │
                   │ (Consumer)   │
                   └──────┬───────┘
                          │
                          │ Update Read Model
                          ▼
                   ┌──────────────┐
                   │    REDIS     │
                   │  Read Model  │
                   │              │
                   │ Key: order:ID│
                   │ Value: JSON  │
                   └──────────────┘
                          │
       ┌──────────────────┘
       │ GET /api/v1/orders/{id}
       ▼
┌─────────────────┐
│  Cliente recibe │
│  Order JSON     │
└─────────────────┘
```

---

##  Conceptos clave para entender

### 1. Partition Key en Kafka
Todos los eventos de un mismo `AggregateId` van a la **misma partición** de Kafka. Esto garantiza **orden** de eventos por aggregate.

### 2. Consumer Groups en Kafka
Múltiples instancias de tu API pueden consumir eventos en paralelo. Cada instancia procesa particiones diferentes.

### 3. TTL en Redis
Los datos en Redis pueden expirar automáticamente. Útil para cache, pero cuidado con Read Models críticos.

### 4. Eventual Consistency
Después de un comando (Write), puede tomar milisegundos hasta que el Read Model en Redis se actualice. Esto es **normal** en CQRS.

---

##  Checklist para producción

- [ ] Configurar replicación de Redis (Master-Slave)
- [ ] Configurar múltiples brokers de Kafka (cluster)
- [ ] Configurar retención de eventos en Kafka según necesidad
- [ ] Implementar Dead Letter Queue (DLQ) para eventos fallidos
- [ ] Monitoreo con Prometheus + Grafana
- [ ] Alertas para consumer lag en Kafka
- [ ] Backup de Redis con RDB o AOF
- [ ] Encriptación en tránsito (TLS)
- [ ] Autenticación Kafka (SASL)
- [ ] Rate limiting en API
- [ ] Circuit breaker para Kafka/Redis
- [ ] Health checks completos

---

##  Recursos adicionales

### Documentación oficial
- Redis: https://redis.io/docs/
- Kafka: https://kafka.apache.org/documentation/
- Confluent Kafka .NET: https://docs.confluent.io/kafka-clients/dotnet/current/overview.html
- StackExchange.Redis: https://stackexchange.github.io/StackExchange.Redis/

### Tutoriales recomendados
- Redis University (gratis): https://university.redis.com/
- Kafka for Beginners: https://www.conduktor.io/kafka/
- Event Sourcing with Kafka: https://www.confluent.io/blog/event-sourcing-cqrs-stream-processing-apache-kafka-whats-connection/

### Herramientas útiles
- RedisInsight (mejor que Redis Commander): https://redis.com/redis-enterprise/redis-insight/
- Offset Explorer (Kafka): https://www.kafkatool.com/
- Conduktor (Kafka IDE): https://www.conduktor.io/

---

¡Ahora estás listo para usar Redis y Kafka en tu arquitectura EDA! 
