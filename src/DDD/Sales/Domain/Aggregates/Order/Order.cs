using DDD.Sales.Domain.ValueObjects;

namespace DDD.Sales.Domain.Aggregates.Order;

/// <summary>
/// Order Aggregate Root
/// Encapsulates all business rules for order management
/// </summary>
public sealed class Order : AggregateRoot
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
    public Money Subtotal { get; private set; }
    public Money Tax { get; private set; }
    public Money Discount { get; private set; }
    public Address ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }

    // Items collection - private set, controlled by aggregate
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Private constructor for EF Core
    private Order() { }

    // Factory method - only way to create an Order
    public static Order Create(CustomerId customerId, Address shippingAddress)
    {
        var order = new Order
        {
            Id = OrderId.Create(),
            CustomerId = customerId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Draft,
            ShippingAddress = shippingAddress,
            Total = Money.Zero(Currency.USD),
            Subtotal = Money.Zero(Currency.USD),
            Tax = Money.Zero(Currency.USD),
            Discount = Money.Zero(Currency.USD),
            CreatedAt = DateTime.UtcNow
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));

        return order;
    }

    /// <summary>
    /// Add item to order
    /// Business Rule: Cannot add items to confirmed orders
    /// Business Rule: Cannot have duplicate products
    /// </summary>
    public void AddItem(ProductId productId, string productName, Money unitPrice, int quantity)
    {
        // Guard clause - cannot modify confirmed orders
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Cannot add items to order in {Status} status");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        // Business rule: check for duplicate product
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            // Update quantity instead of adding duplicate
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var orderItem = OrderItem.Create(Id, productId, productName, unitPrice, quantity);
            _items.Add(orderItem);
        }

        RecalculateTotals();

        AddDomainEvent(new OrderItemAddedEvent(Id, productId, quantity));
    }

    /// <summary>
    /// Remove item from order
    /// Business Rule: Order must have at least one item
    /// </summary>
    public void RemoveItem(OrderItemId itemId)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot remove items from confirmed order");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new InvalidOperationException($"Item {itemId} not found in order");

        _items.Remove(item);

        // Business invariant: order must have at least one item
        if (!_items.Any())
            throw new InvalidOperationException("Order must have at least one item");

        RecalculateTotals();

        AddDomainEvent(new OrderItemRemovedEvent(Id, itemId));
    }

    /// <summary>
    /// Apply discount to order
    /// Business Rule: Discount cannot exceed subtotal
    /// Business Rule: Only one discount per order
    /// </summary>
    public void ApplyDiscount(DiscountId discountId, Money discountAmount)
    {
        if (discountAmount > Subtotal)
            throw new InvalidOperationException("Discount cannot exceed subtotal");

        if (Discount.Amount > 0)
            throw new InvalidOperationException("Order already has a discount applied");

        Discount = discountAmount;
        RecalculateTotals();

        AddDomainEvent(new DiscountAppliedEvent(Id, discountId, discountAmount));
    }

    /// <summary>
    /// Confirm order
    /// Business Rule: Order must have at least one item
    /// Business Rule: Can only confirm draft orders
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");

        if (!_items.Any())
            throw new InvalidOperationException("Cannot confirm order without items");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderConfirmedEvent(Id, Total));
    }

    /// <summary>
    /// Ship order
    /// Business Rule: Only confirmed orders can be shipped
    /// </summary>
    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot ship order in {Status} status");

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderShippedEvent(Id, ShippedAt.Value));
    }

    /// <summary>
    /// Cancel order
    /// Business Rule: Cannot cancel shipped or delivered orders
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel shipped or delivered orders");

        Status = OrderStatus.Cancelled;

        AddDomainEvent(new OrderCancelledEvent(Id, reason));
    }

    /// <summary>
    /// Complete delivery
    /// </summary>
    public void CompleteDelivery()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Can only complete delivery for shipped orders");

        Status = OrderStatus.Delivered;

        AddDomainEvent(new OrderDeliveredEvent(Id, DateTime.UtcNow));
    }

    private void RecalculateTotals()
    {
        Subtotal = _items
            .Select(item => item.GetTotal())
            .Aggregate(Money.Zero(Currency.USD), (acc, money) => acc.Add(money));

        // Tax rate: 15% (could come from configuration or tax service)
        Tax = Subtotal.Multiply(0.15m);

        Total = Subtotal.Add(Tax).Subtract(Discount);
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"ORD-{timestamp}-{random}";
    }
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
