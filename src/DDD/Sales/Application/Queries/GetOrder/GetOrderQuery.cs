using DDD.Sales.Application.DTOs;
using MediatR;

namespace DDD.Sales.Application.Queries.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var orderId = DDD.Sales.Domain.ValueObjects.OrderId.CreateFrom(request.OrderId);
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order == null)
            return null;

        return new OrderDto
        {
            Id = order.Id.Value,
            CustomerId = order.CustomerId.Value,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Total = order.Total.Amount,
            Currency = order.Total.Currency.ToString(),
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId.Value,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice.Amount,
                Quantity = i.Quantity,
                Total = i.GetTotal().Amount
            }).ToList(),
            ShippingAddress = new AddressDto
            {
                Street = order.ShippingAddress.Street,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                Country = order.ShippingAddress.Country,
                ZipCode = order.ShippingAddress.ZipCode
            },
            CreatedAt = order.CreatedAt
        };
    }
}
