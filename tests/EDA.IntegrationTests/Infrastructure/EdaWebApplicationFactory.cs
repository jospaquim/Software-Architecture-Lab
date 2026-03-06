using EDA.Infrastructure.EventStore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace EDA.IntegrationTests.Infrastructure;

public class EdaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace Event Store with in-memory implementation for testing
            services.RemoveAll(typeof(IEventStore));
            services.AddSingleton<IEventStore, InMemoryEventStore>();

            // Replace Read Model Repository with in-memory implementation
            services.RemoveAll(typeof(IReadModelRepository));
            services.AddSingleton<IReadModelRepository, InMemoryReadModelRepository>();
        });

        builder.UseEnvironment("Testing");
    }
}

// In-memory Event Store for testing
public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<IEvent>> _events = new();
    private readonly object _lock = new();

    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_events.ContainsKey(aggregateId))
            {
                _events[aggregateId] = new List<IEvent>();
            }

            var currentVersion = _events[aggregateId].Count;
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException($"Concurrency conflict. Expected version {expectedVersion}, but found {currentVersion}");
            }

            var version = currentVersion;
            foreach (var @event in events)
            {
                @event.Version = ++version;
                @event.Timestamp = DateTime.UtcNow;
                _events[aggregateId].Add(@event);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_events.TryGetValue(aggregateId, out var events))
            {
                return Task.FromResult<IEnumerable<IEvent>>(events.ToList());
            }

            return Task.FromResult<IEnumerable<IEvent>>(new List<IEvent>());
        }
    }

    public Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_events.TryGetValue(aggregateId, out var events))
            {
                var filtered = events.Where(e => e.Version > fromVersion).ToList();
                return Task.FromResult<IEnumerable<IEvent>>(filtered);
            }

            return Task.FromResult<IEnumerable<IEvent>>(new List<IEvent>());
        }
    }
}

// In-memory Read Model Repository for testing
public class InMemoryReadModelRepository : IReadModelRepository
{
    private readonly Dictionary<Guid, OrderReadModel> _orders = new();
    private readonly object _lock = new();

    public Task SaveOrderAsync(OrderReadModel order, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _orders[order.OrderId] = order;
        }

        return Task.CompletedTask;
    }

    public Task<OrderReadModel?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _orders.TryGetValue(orderId, out var order);
            return Task.FromResult(order);
        }
    }

    public Task<IEnumerable<OrderReadModel>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<OrderReadModel>>(_orders.Values.ToList());
        }
    }

    public Task DeleteOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _orders.Remove(orderId);
        }

        return Task.CompletedTask;
    }
}
