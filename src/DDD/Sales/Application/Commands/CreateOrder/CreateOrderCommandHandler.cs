using DDD.Sales.Domain.Aggregates.Order;
using DDD.Sales.Domain.ValueObjects;
using MediatR;

namespace DDD.Sales.Application.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create value objects
            var customerId = CustomerId.CreateFrom(request.CustomerId);
            var address = Address.Create(
                request.Street,
                request.City,
                request.State,
                request.Country,
                request.ZipCode);

            // Create order aggregate
            var order = Order.Create(customerId, address);

            // Persist
            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(order.Id.Value);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Repository interface - defined in Domain but implemented in Infrastructure
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
