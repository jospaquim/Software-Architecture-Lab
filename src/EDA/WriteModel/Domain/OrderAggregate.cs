using EDA.EventStore;

namespace EDA.WriteModel.Domain;

/// <summary>
/// Order Aggregate with Event Sourcing
/// State is rebuilt from events instead of stored directly
/// </summary>
public class OrderAggregate
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
    public int Version { get; private set; }

    private readonly List<IEvent> _uncommittedEvents = new();

    public IReadOnlyCollection<IEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    // For reconstruction from events
    private OrderAggregate()
    {
    }

    // Factory method for new order
    public static OrderAggregate CreateNew(Guid customerId)
    {
        var order = new OrderAggregate();
        order.ApplyChange(new OrderCreatedEvent
        {
            AggregateId = Guid.NewGuid(),
            CustomerId = customerId,
            Version = 1
        });

        return order;
    }

    // Load from event history
    public static OrderAggregate LoadFromHistory(IEnumerable<IEvent> history)
    {
        var order = new OrderAggregate();

        foreach (var @event in history)
        {
            order.ApplyEvent(@event, isNew: false);
        }

        return order;
    }

    // Public methods - commands
    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify confirmed order");

        ApplyChange(new ItemAddedEvent
        {
            AggregateId = Id,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Version = Version + 1
        });
    }

    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");

        if (!Items.Any())
            throw new InvalidOperationException("Cannot confirm empty order");

        ApplyChange(new OrderConfirmedEvent
        {
            AggregateId = Id,
            Total = Total,
            Version = Version + 1
        });
    }

    public void ShipOrder()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Can only ship confirmed orders");

        ApplyChange(new OrderShippedEvent
        {
            AggregateId = Id,
            ShippedAt = DateTime.UtcNow,
            Version = Version + 1
        });
    }

    public void CancelOrder(string reason)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel shipped or delivered orders");

        ApplyChange(new OrderCancelledEvent
        {
            AggregateId = Id,
            Reason = reason,
            Version = Version + 1
        });
    }

    // Apply new event
    private void ApplyChange(IEvent @event)
    {
        ApplyEvent(@event, isNew: true);
    }

    // Apply event to state
    private void ApplyEvent(IEvent @event, bool isNew)
    {
        // Dynamic dispatch to appropriate Apply method
        Apply((dynamic)@event);

        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }

        Version = @event.Version;
    }

    // Event handlers - rebuild state
    private void Apply(OrderCreatedEvent @event)
    {
        Id = @event.AggregateId;
        CustomerId = @event.CustomerId;
        Status = OrderStatus.Draft;
        Total = 0;
    }

    private void Apply(ItemAddedEvent @event)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == @event.ProductId);

        if (existing != null)
        {
            existing.Quantity += @event.Quantity;
        }
        else
        {
            Items.Add(new OrderItem
            {
                ProductId = @event.ProductId,
                ProductName = @event.ProductName,
                UnitPrice = @event.UnitPrice,
                Quantity = @event.Quantity
            });
        }

        RecalculateTotal();
    }

    private void Apply(OrderConfirmedEvent @event)
    {
        Status = OrderStatus.Confirmed;
    }

    private void Apply(OrderShippedEvent @event)
    {
        Status = OrderStatus.Shipped;
    }

    private void Apply(OrderCancelledEvent @event)
    {
        Status = OrderStatus.Cancelled;
    }

    private void RecalculateTotal()
    {
        Total = Items.Sum(i => i.UnitPrice * i.Quantity);
    }

    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

// Events
public record OrderCreatedEvent : DomainEvent
{
    public Guid CustomerId { get; init; }
}

public record ItemAddedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public record OrderConfirmedEvent : DomainEvent
{
    public decimal Total { get; init; }
}

public record OrderShippedEvent : DomainEvent
{
    public DateTime ShippedAt { get; init; }
}

public record OrderCancelledEvent : DomainEvent
{
    public string Reason { get; init; } = string.Empty;
}
