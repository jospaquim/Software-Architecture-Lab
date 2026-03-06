using Confluent.Kafka;
using System.Text.Json;
using EDA.EventStore;

namespace EDA.Infrastructure.Kafka;

/// <summary>
/// Event Bus usando Kafka para Pub/Sub
/// Publica eventos que múltiples servicios pueden consumir
///
/// CARACTERÍSTICAS:
/// - Publicación asíncrona de eventos
/// - Múltiples suscriptores (consumer groups)
/// - Background consumer para procesar eventos
/// - Retry automático en caso de fallo
///
/// USO:
/// 1. Publicar eventos: await eventBus.PublishAsync(event)
/// 2. Suscribirse a eventos: eventBus.Subscribe<OrderCreatedEvent>(handler)
/// 3. Iniciar consumer: eventBus.StartConsuming(cancellationToken)
/// </summary>
public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly string _bootstrapServers;
    private readonly string _groupId;
    private readonly Dictionary<string, List<Func<IEvent, Task>>> _handlers = new();
    private readonly SemaphoreSlim _handlerLock = new(1, 1);

    public KafkaEventBus(IConfiguration configuration)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:Topics:Orders"] ?? "orders-events";
        _groupId = configuration["Kafka:GroupId"] ?? "eda-order-service";

        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                Console.WriteLine($" Kafka Event Bus Error: {error.Reason}"))
            .Build();
    }

    /// <summary>
    /// Publica un evento a Kafka
    /// El evento será consumido por todos los subscribers
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
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
                PublishedAt = DateTime.UtcNow
            })
        };

        var json = JsonSerializer.Serialize(eventWrapper);

        var message = new Message<string, string>
        {
            Key = @event.AggregateId.ToString(),
            Value = json,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.EventType) },
                { "aggregate-id", System.Text.Encoding.UTF8.GetBytes(@event.AggregateId.ToString()) },
                { "event-id", System.Text.Encoding.UTF8.GetBytes(@event.EventId.ToString()) }
            }
        };

        try
        {
            var result = await _producer.ProduceAsync(_topic, message);
            Console.WriteLine($" Event {@event.EventType} published to partition {result.Partition} at offset {result.Offset}");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($" Failed to publish event {@event.EventType}: {ex.Error.Reason}");
            throw new InvalidOperationException($"Failed to publish event to Kafka: {ex.Error.Reason}", ex);
        }
    }

    /// <summary>
    /// Suscribirse a eventos de un tipo específico
    /// El handler será invocado cuando se reciba un evento de ese tipo
    /// </summary>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        _handlerLock.Wait();
        try
        {
            var eventType = typeof(TEvent).Name;

            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Func<IEvent, Task>>();
                Console.WriteLine($" Registered handler for {eventType}");
            }

            _handlers[eventType].Add(async (@event) =>
            {
                if (@event is TEvent typedEvent)
                {
                    await handler(typedEvent);
                }
            });
        }
        finally
        {
            _handlerLock.Release();
        }
    }

    /// <summary>
    /// Inicia el consumo de eventos en background
    /// Debe ser llamado al arrancar la aplicación
    /// </summary>
    public void StartConsuming(CancellationToken cancellationToken)
    {
        Task.Run(async () => await ConsumeEventsAsync(cancellationToken), cancellationToken);
        Console.WriteLine($" Kafka Event Bus consumer started (Group: {_groupId})");
    }

    private async Task ConsumeEventsAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Latest, // Solo eventos nuevos
            EnableAutoCommit = false, // Commit manual después de procesar
            SessionTimeoutMs = 10000,
            HeartbeatIntervalMs = 3000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                Console.WriteLine($" Kafka Consumer Error: {error.Reason}"))
            .SetPartitionsAssignedHandler((_, partitions) =>
                Console.WriteLine($" Assigned partitions: {string.Join(", ", partitions)}"))
            .Build();

        consumer.Subscribe(_topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(cancellationToken);

                    if (result == null || result.IsPartitionEOF)
                        continue;

                    await ProcessMessageAsync(result.Message, cancellationToken);

                    // Commit offset después de procesar exitosamente
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($" Error consuming message: {ex.Error.Reason}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Error processing message: {ex.Message}");
                    // No hacer commit si falló el procesamiento
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(" Event Bus consumer stopped");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(Message<string, string> message, CancellationToken cancellationToken)
    {
        try
        {
            // Deserializar evento
            var wrapper = JsonSerializer.Deserialize<KafkaEventWrapper>(message.Value);
            if (wrapper == null)
            {
                Console.WriteLine("️ Failed to deserialize event wrapper");
                return;
            }

            var metadata = JsonSerializer.Deserialize<EventMetadata>(wrapper.Metadata ?? "{}");
            var eventType = Type.GetType(metadata?.ClrType ?? wrapper.EventType);

            if (eventType == null)
            {
                Console.WriteLine($"️ Unknown event type: {wrapper.EventType}");
                return;
            }

            var @event = JsonSerializer.Deserialize(wrapper.EventData, eventType) as IEvent;
            if (@event == null)
            {
                Console.WriteLine($"️ Failed to deserialize event {wrapper.EventId}");
                return;
            }

            // Buscar handlers para este tipo de evento
            await _handlerLock.WaitAsync(cancellationToken);
            List<Func<IEvent, Task>>? handlers = null;

            try
            {
                _handlers.TryGetValue(eventType.Name, out handlers);
            }
            finally
            {
                _handlerLock.Release();
            }

            if (handlers == null || !handlers.Any())
            {
                // No hay handlers registrados, esto es normal
                return;
            }

            // Ejecutar todos los handlers
            Console.WriteLine($" Processing event {eventType.Name} (ID: {@event.EventId})");

            var tasks = handlers.Select(handler => ExecuteHandlerSafelyAsync(handler, @event));
            await Task.WhenAll(tasks);

            Console.WriteLine($" Event {eventType.Name} processed by {handlers.Count} handler(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error processing message: {ex.Message}");
            throw; // Re-throw para que no se haga commit
        }
    }

    private async Task ExecuteHandlerSafelyAsync(Func<IEvent, Task> handler, IEvent @event)
    {
        try
        {
            await handler(@event);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Handler failed for event {@event.EventId}: {ex.Message}");
            // No re-throw para que otros handlers puedan ejecutarse
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        _handlerLock?.Dispose();
    }

    private class EventMetadata
    {
        public string? ClrType { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
