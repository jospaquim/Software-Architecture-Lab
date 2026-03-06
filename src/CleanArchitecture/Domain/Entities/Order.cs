using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Order entity - Aggregate root for orders
/// Implements business rules and invariants
/// </summary>
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }

    public string? Notes { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Foreign keys
    public int CustomerId { get; set; }
    public int? ShippingAddressId { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Address? ShippingAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    // Business rules and invariants

    /// <summary>
    /// Adds an item to the order
    /// Ensures business rule: Order cannot be modified after shipped
    /// </summary>
    public void AddItem(Product product, int quantity, decimal unitPrice)
    {
        if (Status >= OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot modify order after it has been shipped");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Order = this
            };

            Items.Add(orderItem);
        }

        RecalculateTotals();
    }

    /// <summary>
    /// Removes an item from the order
    /// </summary>
    public void RemoveItem(int orderItemId)
    {
        if (Status >= OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot modify order after it has been shipped");

        var item = Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotals();
        }
    }

    /// <summary>
    /// Updates item quantity
    /// </summary>
    public void UpdateItemQuantity(int orderItemId, int newQuantity)
    {
        if (Status >= OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot modify order after it has been shipped");

        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        var item = Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item != null)
        {
            item.Quantity = newQuantity;
            RecalculateTotals();
        }
    }

    /// <summary>
    /// Applies discount to the order (VIP customers, promotions, etc.)
    /// </summary>
    public void ApplyDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(discountAmount));

        if (discountAmount > Subtotal)
            throw new ArgumentException("Discount cannot be greater than subtotal", nameof(discountAmount));

        Discount = discountAmount;
        RecalculateTotals();
    }

    /// <summary>
    /// Calculate totals based on items
    /// </summary>
    private void RecalculateTotals()
    {
        Subtotal = Items.Sum(item => item.GetTotalPrice());
        Tax = Subtotal * 0.15m; // 15% tax rate (this could come from configuration)
        Total = Subtotal + Tax - Discount;
    }

    /// <summary>
    /// Confirms the order and moves it to processing
    /// </summary>
    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");

        if (!Items.Any())
            throw new InvalidOperationException("Cannot confirm order without items");

        Status = OrderStatus.Processing;
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber, CustomerId, Total));
    }

    /// <summary>
    /// Marks the order as shipped
    /// </summary>
    public void Ship()
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException($"Cannot ship order in {Status} status");

        if (PaymentStatus != PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot ship order with incomplete payment");

        Status = OrderStatus.Shipped;
        ShippedDate = DateTime.UtcNow;

        AddDomainEvent(new OrderShippedEvent(Id, OrderNumber, CustomerId, ShippedDate.Value));
    }

    /// <summary>
    /// Marks the order as delivered
    /// </summary>
    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot deliver order in {Status} status");

        Status = OrderStatus.Delivered;
        DeliveredDate = DateTime.UtcNow;

        AddDomainEvent(new OrderDeliveredEvent(Id, OrderNumber, CustomerId, DeliveredDate.Value));
    }

    /// <summary>
    /// Cancels the order
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status >= OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel order after it has been shipped");

        Status = OrderStatus.Cancelled;
        Notes = $"Cancelled: {reason}";

        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, CustomerId, reason));
    }

    /// <summary>
    /// Processes payment for the order
    /// </summary>
    public void ProcessPayment()
    {
        if (PaymentStatus == PaymentStatus.Completed)
            throw new InvalidOperationException("Payment has already been completed");

        PaymentStatus = PaymentStatus.Completed;
        AddDomainEvent(new PaymentCompletedEvent(Id, OrderNumber, Total, PaymentMethod));
    }

    /// <summary>
    /// Generates a unique order number
    /// </summary>
    public static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

public class OrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;

    // Foreign keys
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal GetTotalPrice() => UnitPrice * Quantity;
}

// Domain Events
public class OrderConfirmedEvent : DomainEvent
{
    public int OrderId { get; }
    public string OrderNumber { get; }
    public int CustomerId { get; }
    public decimal Total { get; }

    public OrderConfirmedEvent(int orderId, string orderNumber, int customerId, decimal total)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Total = total;
    }
}

public class OrderShippedEvent : DomainEvent
{
    public int OrderId { get; }
    public string OrderNumber { get; }
    public int CustomerId { get; }
    public DateTime ShippedDate { get; }

    public OrderShippedEvent(int orderId, string orderNumber, int customerId, DateTime shippedDate)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        ShippedDate = shippedDate;
    }
}

public class OrderDeliveredEvent : DomainEvent
{
    public int OrderId { get; }
    public string OrderNumber { get; }
    public int CustomerId { get; }
    public DateTime DeliveredDate { get; }

    public OrderDeliveredEvent(int orderId, string orderNumber, int customerId, DateTime deliveredDate)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        DeliveredDate = deliveredDate;
    }
}

public class OrderCancelledEvent : DomainEvent
{
    public int OrderId { get; }
    public string OrderNumber { get; }
    public int CustomerId { get; }
    public string Reason { get; }

    public OrderCancelledEvent(int orderId, string orderNumber, int customerId, string reason)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Reason = reason;
    }
}

public class PaymentCompletedEvent : DomainEvent
{
    public int OrderId { get; }
    public string OrderNumber { get; }
    public decimal Amount { get; }
    public PaymentMethod PaymentMethod { get; }

    public PaymentCompletedEvent(int orderId, string orderNumber, decimal amount, PaymentMethod paymentMethod)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        Amount = amount;
        PaymentMethod = paymentMethod;
    }
}
