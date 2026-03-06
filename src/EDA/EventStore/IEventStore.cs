namespace EDA.EventStore;

/// <summary>
/// Event Store - Stores all events that have occurred in the system
/// Core of Event Sourcing architecture
/// </summary>
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, int expectedVersion);
    Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId);
    Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion);
    Task<IEnumerable<IEvent>> GetAllEventsAsync(DateTime? from = null, DateTime? to = null);
}

/// <summary>
/// Base interface for all events
/// </summary>
public interface IEvent
{
    Guid EventId { get; }
    Guid AggregateId { get; }
    int Version { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract record DomainEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid AggregateId { get; init; }
    public int Version { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

/// <summary>
/// Event wrapper for persistence
/// </summary>
public class EventWrapper
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty; // JSON serialized
    public string? Metadata { get; set; }
}
