using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Product entity - Represents a product in the inventory
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }

    // Navigation properties
    public Category Category { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Business rules
    public bool IsInStock() => Stock > 0;

    public bool CanFulfillOrder(int quantity) => Stock >= quantity;

    public void DecreaseStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(quantity));

        if (!CanFulfillOrder(quantity))
            throw new InvalidOperationException($"Insufficient stock. Available: {Stock}, Requested: {quantity}");

        Stock -= quantity;

        if (Stock == 0)
            AddDomainEvent(new ProductOutOfStockEvent(Id, Name, Sku));
        else if (Stock <= 10)
            AddDomainEvent(new ProductLowStockEvent(Id, Name, Sku, Stock));
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(quantity));

        Stock += quantity;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));

        var oldPrice = Price;
        Price = newPrice;

        if (oldPrice != newPrice)
            AddDomainEvent(new ProductPriceChangedEvent(Id, Name, oldPrice, newPrice));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// Domain Events
public class ProductOutOfStockEvent : DomainEvent
{
    public int ProductId { get; }
    public string ProductName { get; }
    public string Sku { get; }

    public ProductOutOfStockEvent(int productId, string productName, string sku)
    {
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
    }
}

public class ProductLowStockEvent : DomainEvent
{
    public int ProductId { get; }
    public string ProductName { get; }
    public string Sku { get; }
    public int CurrentStock { get; }

    public ProductLowStockEvent(int productId, string productName, string sku, int currentStock)
    {
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        CurrentStock = currentStock;
    }
}

public class ProductPriceChangedEvent : DomainEvent
{
    public int ProductId { get; }
    public string ProductName { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }

    public ProductPriceChangedEvent(int productId, string productName, decimal oldPrice, decimal newPrice)
    {
        ProductId = productId;
        ProductName = productName;
        OldPrice = oldPrice;
        NewPrice = newPrice;
    }
}
