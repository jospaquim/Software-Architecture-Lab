namespace DDD.Sales.Domain.Aggregates;

/// <summary>
/// Base class for all Aggregate Roots
/// Manages domain events and provides common functionality
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // For Event Sourcing support
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    protected void ApplyChange(IDomainEvent @event, bool isNew = true)
    {
        // Apply event to state using dynamic dispatch
        ((dynamic)this).Apply((dynamic)@event);

        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }

    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }
}

/// <summary>
/// Base interface for all Domain Events
/// Events are facts that have happened in the domain
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base class for Domain Events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
