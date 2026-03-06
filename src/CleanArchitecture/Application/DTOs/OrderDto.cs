using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.DTOs;

public class OrderDto
{
    public Guid Uid { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public Guid CustomerUid { get; set; }
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
    public Guid Uid { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class OrderItemDto
{
    public Guid Uid { get; set; }
    public Guid ProductUid { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderDto
{
    public Guid CustomerUid { get; set; }
    public Guid? ShippingAddressUid { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
}

public class CreateOrderItemDto
{
    public Guid ProductUid { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public Guid OrderUid { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}
