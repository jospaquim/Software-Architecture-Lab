using DDD.Sales.Domain.ValueObjects;
using MediatR;

namespace DDD.Sales.Application.Commands.AddItemToOrder;

public class AddItemToOrderCommandHandler : IRequestHandler<AddItemToOrderCommand, Result<bool>>
{
    private readonly IOrderRepository _orderRepository;

    public AddItemToOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<bool>> Handle(AddItemToOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var orderId = OrderId.CreateFrom(request.OrderId);
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

            if (order == null)
                return Result<bool>.Failure("Order not found");

            var productId = ProductId.CreateFrom(request.ProductId);
            var currency = Enum.Parse<Currency>(request.Currency);
            var unitPrice = Money.Create(request.UnitPrice, currency);

            order.AddItem(productId, request.ProductName, unitPrice, request.Quantity);

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}
