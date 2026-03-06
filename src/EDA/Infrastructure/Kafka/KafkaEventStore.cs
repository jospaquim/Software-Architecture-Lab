using Confluent.Kafka;
using System.Text.Json;
using EDA.EventStore;

namespace EDA.Infrastructure.Kafka;

/// <summary>
/// Event Store usando Kafka
/// Los eventos se almacenan como mensajes en topics de Kafka
/// Kafka garantiza orden y persistencia de eventos
///
/// VENTAJAS:
/// - Persistencia durable de eventos
/// - Replay de eventos desde cualquier punto
/// - Alto throughput (millones de eventos/segundo)
/// - Múltiples consumidores independientes
///
/// USO:
/// - Reemplazar InMemoryEventStore en Program.cs
/// - Configurar Kafka en appsettings.json
/// </summary>
public class KafkaEventStore : IEventStore, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly string _bootstrapServers;

    public KafkaEventStore(IConfiguration configuration)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:Topics:Orders"] ?? "orders-events";

        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            // Configuración para garantizar durabilidad
            Acks = Acks.All, // Esperar confirmación de todos los replicas
            EnableIdempotence = true, // Prevenir duplicados
            MessageSendMaxRetries = 3,
            // Configuración de performance
            CompressionType = CompressionType.Snappy,
            BatchSize = 16384,
            LingerMs = 10 // Esperar 10ms para agrupar mensajes
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                Console.WriteLine($"Kafka Producer Error: {error.Reason}"))
            .Build();
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
                    { "aggregate-id", System.Text.Encoding.UTF8.GetBytes(aggregateId.ToString()) },
                    { "version", System.Text.Encoding.UTF8.GetBytes(@event.Version.ToString()) }
                }
            };

            try
            {
                var result = await _producer.ProduceAsync(_topic, message);
                Console.WriteLine($" Event {@event.EventType} (v{@event.Version}) saved to Kafka partition {result.Partition} at offset {result.Offset}");
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($" Failed to save event {@event.EventType}: {ex.Error.Reason}");
                throw new InvalidOperationException($"Failed to save event to Kafka: {ex.Error.Reason}", ex);
            }
        }

        // Asegurar que todos los mensajes se envíen
        _producer.Flush(TimeSpan.FromSeconds(10));
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId)
    {
        return await GetEventsAsync(aggregateId, fromVersion: 1);
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion)
    {
        var events = new List<IEvent>();
        var key = aggregateId.ToString();

        // Crear consumer temporal para leer eventos
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = $"event-reader-{Guid.NewGuid()}", // Grupo único para no afectar otros consumers
            AutoOffsetReset = AutoOffsetReset.Earliest, // Leer desde el principio
            EnableAutoCommit = false // No commitear offsets
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        try
        {
            // Leer eventos del topic
            // Timeout para no esperar infinitamente
            var timeout = TimeSpan.FromSeconds(5);
            var endTime = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < endTime)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));

                if (consumeResult == null) continue;

                // Filtrar solo eventos del aggregate que buscamos
                if (consumeResult.Message.Key != key) continue;

                try
                {
                    var wrapper = JsonSerializer.Deserialize<KafkaEventWrapper>(consumeResult.Message.Value);
                    if (wrapper == null || wrapper.Version < fromVersion) continue;

                    var @event = DeserializeEvent(wrapper);
                    events.Add(@event);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"️ Failed to deserialize event: {ex.Message}");
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        return events.OrderBy(e => e.Version);
    }

    public async Task<IEnumerable<IEvent>> GetAllEventsAsync(DateTime? from = null, DateTime? to = null)
    {
        var events = new List<IEvent>();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = $"all-events-reader-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        try
        {
            var timeout = TimeSpan.FromSeconds(10);
            var endTime = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < endTime)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult == null) continue;

                try
                {
                    var wrapper = JsonSerializer.Deserialize<KafkaEventWrapper>(consumeResult.Message.Value);
                    if (wrapper == null) continue;

                    // Filtrar por rango de fechas
                    if (from.HasValue && wrapper.OccurredAt < from.Value) continue;
                    if (to.HasValue && wrapper.OccurredAt > to.Value) continue;

                    var @event = DeserializeEvent(wrapper);
                    events.Add(@event);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"️ Failed to deserialize event: {ex.Message}");
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        return events.OrderBy(e => e.OccurredAt);
    }

    private IEvent DeserializeEvent(KafkaEventWrapper wrapper)
    {
        var metadata = JsonSerializer.Deserialize<EventMetadata>(wrapper.Metadata ?? "{}");
        var eventType = Type.GetType(metadata?.ClrType ?? wrapper.EventType);

        if (eventType == null)
        {
            throw new InvalidOperationException($"Cannot deserialize event type: {wrapper.EventType}. " +
                "Ensure the event type is available in the current assembly.");
        }

        var @event = JsonSerializer.Deserialize(wrapper.EventData, eventType) as IEvent;

        if (@event == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {wrapper.EventId}");
        }

        return @event;
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }

    private class EventMetadata
    {
        public string? ClrType { get; set; }
        public DateTime SavedAt { get; set; }
        public int ExpectedVersion { get; set; }
    }
}

/// <summary>
/// Wrapper para eventos almacenados en Kafka
/// Contiene metadata adicional para deserialización
/// </summary>
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
