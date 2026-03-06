using System.Collections.Concurrent;
using System.Text.Json;

namespace EDA.EventStore;

/// <summary>
/// In-Memory Event Store implementation
/// For production, use EventStoreDB, Kafka, or SQL-based event store
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<EventWrapper>> _events = new();
    private readonly object _lock = new();

    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, int expectedVersion)
    {
        lock (_lock)
        {
            var eventsList = events.ToList();

            if (!eventsList.Any())
                return Task.CompletedTask;

            // Get existing events for aggregate
            if (!_events.TryGetValue(aggregateId, out var existingEvents))
            {
                existingEvents = new List<EventWrapper>();
                _events[aggregateId] = existingEvents;
            }

            // Optimistic concurrency check
            var currentVersion = existingEvents.Any() ? existingEvents.Max(e => e.Version) : 0;

            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(
                    $"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but current version is {currentVersion}");
            }

            // Add new events
            foreach (var @event in eventsList)
            {
                var wrapper = new EventWrapper
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
                        SavedAt = DateTime.UtcNow
                    })
                };

                existingEvents.Add(wrapper);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId)
    {
        if (!_events.TryGetValue(aggregateId, out var events))
        {
            return Task.FromResult(Enumerable.Empty<IEvent>());
        }

        var domainEvents = events
            .OrderBy(e => e.Version)
            .Select(DeserializeEvent)
            .ToList();

        return Task.FromResult<IEnumerable<IEvent>>(domainEvents);
    }

    public Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion)
    {
        if (!_events.TryGetValue(aggregateId, out var events))
        {
            return Task.FromResult(Enumerable.Empty<IEvent>());
        }

        var domainEvents = events
            .Where(e => e.Version >= fromVersion)
            .OrderBy(e => e.Version)
            .Select(DeserializeEvent)
            .ToList();

        return Task.FromResult<IEnumerable<IEvent>>(domainEvents);
    }

    public Task<IEnumerable<IEvent>> GetAllEventsAsync(DateTime? from = null, DateTime? to = null)
    {
        var allEvents = _events.Values
            .SelectMany(e => e)
            .Where(e => (!from.HasValue || e.OccurredAt >= from.Value) &&
                       (!to.HasValue || e.OccurredAt <= to.Value))
            .OrderBy(e => e.OccurredAt)
            .Select(DeserializeEvent)
            .ToList();

        return Task.FromResult<IEnumerable<IEvent>>(allEvents);
    }

    private IEvent DeserializeEvent(EventWrapper wrapper)
    {
        var metadata = JsonSerializer.Deserialize<EventMetadata>(wrapper.Metadata ?? "{}");
        var eventType = Type.GetType(metadata?.ClrType ?? wrapper.EventType);

        if (eventType == null)
        {
            throw new InvalidOperationException($"Cannot deserialize event type: {wrapper.EventType}");
        }

        var @event = JsonSerializer.Deserialize(wrapper.EventData, eventType) as IEvent;

        if (@event == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {wrapper.EventId}");
        }

        return @event;
    }

    private class EventMetadata
    {
        public string? ClrType { get; set; }
        public DateTime SavedAt { get; set; }
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message)
    {
    }
}
