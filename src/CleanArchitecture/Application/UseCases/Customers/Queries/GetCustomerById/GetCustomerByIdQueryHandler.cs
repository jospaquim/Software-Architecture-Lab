using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Interfaces;
using Mapster;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailsDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCustomerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerDetailsDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _unitOfWork.Customers.GetByUidAsync(request.CustomerUid, cancellationToken);

        if (customer == null)
        {
            return Result<CustomerDetailsDto>.Failure($"Customer with UID {request.CustomerUid} not found");
        }

        var addresses = await _unitOfWork.Addresses.GetCustomerAddressesAsync(customer.Id, cancellationToken);
        var orders = await _unitOfWork.Orders.GetCustomerOrdersAsync(customer.Id, cancellationToken);
        var recentOrders = orders.Take(10).ToList();

        var customerDto = customer.Adapt<CustomerDetailsDto>();
        customerDto.Addresses = addresses.Adapt<List<AddressDto>>();
        customerDto.RecentOrders = recentOrders.Adapt<List<OrderSummaryDto>>();

        return Result<CustomerDetailsDto>.Success(customerDto);
    }
}
