using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class OrderDetailsDto : OrderDto
{
    public CustomerDto Customer { get; set; } = null!;
    public AddressDto? ShippingAddress { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public int? ShippingAddressId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
}

public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public int OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}
