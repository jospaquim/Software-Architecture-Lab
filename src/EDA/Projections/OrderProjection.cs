using EDA.EventStore;
using EDA.ReadModel;
using EDA.WriteModel.Domain;

namespace EDA.Projections;

/// <summary>
/// Projection - Updates Read Model when events occur
/// Implements Eventually Consistent reads
/// </summary>
public interface IProjection
{
    Task ProjectAsync(IEvent @event);
}

public class OrderProjection : IProjection
{
    private readonly IOrderReadModelRepository _readRepository;

    public OrderProjection(IOrderReadModelRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task ProjectAsync(IEvent @event)
    {
        // Dynamic dispatch to appropriate handler
        await ((dynamic)this).HandleAsync((dynamic)@event);
    }

    private async Task HandleAsync(OrderCreatedEvent @event)
    {
        var readModel = new OrderReadModel
        {
            Id = @event.AggregateId,
            CustomerId = @event.CustomerId,
            Status = "Draft",
            Total = 0,
            ItemCount = 0,
            CreatedAt = @event.OccurredAt,
            Items = new List<OrderItemReadModel>()
        };

        await _readRepository.SaveAsync(readModel);
    }

    private async Task HandleAsync(ItemAddedEvent @event)
    {
        var readModel = await _readRepository.GetByIdAsync(@event.AggregateId);

        if (readModel == null)
            return; // Should not happen

        var existingItem = readModel.Items.FirstOrDefault(i => i.ProductId == @event.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += @event.Quantity;
            existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
        }
        else
        {
            readModel.Items.Add(new OrderItemReadModel
            {
                ProductId = @event.ProductId,
                ProductName = @event.ProductName,
                UnitPrice = @event.UnitPrice,
                Quantity = @event.Quantity,
                TotalPrice = @event.UnitPrice * @event.Quantity
            });
        }

        readModel.ItemCount = readModel.Items.Sum(i => i.Quantity);
        readModel.Total = readModel.Items.Sum(i => i.TotalPrice);

        await _readRepository.UpdateAsync(readModel);
    }

    private async Task HandleAsync(OrderConfirmedEvent @event)
    {
        var readModel = await _readRepository.GetByIdAsync(@event.AggregateId);

        if (readModel == null)
            return;

        readModel.Status = "Confirmed";
        readModel.ConfirmedAt = @event.OccurredAt;
        readModel.Total = @event.Total;

        await _readRepository.UpdateAsync(readModel);
    }

    private async Task HandleAsync(OrderShippedEvent @event)
    {
        var readModel = await _readRepository.GetByIdAsync(@event.AggregateId);

        if (readModel == null)
            return;

        readModel.Status = "Shipped";
        readModel.ShippedAt = @event.ShippedAt;

        await _readRepository.UpdateAsync(readModel);
    }

    private async Task HandleAsync(OrderCancelledEvent @event)
    {
        var readModel = await _readRepository.GetByIdAsync(@event.AggregateId);

        if (readModel == null)
            return;

        readModel.Status = "Cancelled";

        await _readRepository.UpdateAsync(readModel);
    }

    // Default handler for unknown events
    private Task HandleAsync(IEvent @event)
    {
        // Ignore unknown events
        return Task.CompletedTask;
    }
}

/// <summary>
/// Event Bus - Publishes events to all projections
/// In production, use RabbitMQ, Kafka, or Azure Service Bus
/// </summary>
public interface IEventBus
{
    Task PublishAsync(IEvent @event);
    void Subscribe(IProjection projection);
}

public class InMemoryEventBus : IEventBus
{
    private readonly List<IProjection> _projections = new();

    public void Subscribe(IProjection projection)
    {
        _projections.Add(projection);
    }

    public async Task PublishAsync(IEvent @event)
    {
        foreach (var projection in _projections)
        {
            await projection.ProjectAsync(@event);
        }
    }
}
