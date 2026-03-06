using DDD.Sales.Domain.ValueObjects;

namespace DDD.Sales.Domain.Aggregates.Order;

/// <summary>
/// OrderItem Entity - Part of Order Aggregate
/// NOT an Aggregate Root - only accessible through Order
/// </summary>
public sealed class OrderItem
{
    public OrderItemId Id { get; private set; }
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    internal static OrderItem Create(OrderId orderId, ProductId productId, string productName, Money unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required", nameof(productName));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        return new OrderItem
        {
            Id = OrderItemId.Create(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }

    internal void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Quantity += amount;
    }

    internal void DecreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (Quantity - amount < 0)
            throw new InvalidOperationException("Cannot decrease quantity below zero");

        Quantity -= amount;
    }

    public Money GetTotal() => UnitPrice.Multiply(Quantity);
}
