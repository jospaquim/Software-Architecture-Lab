using AutoMapper;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCustomerByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDetailsDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        // Get customer with related data
        var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer == null)
        {
            return Result<CustomerDetailsDto>.Failure($"Customer with ID {request.CustomerId} not found");
        }

        // Get customer addresses
        var addresses = await _unitOfWork.Addresses.GetCustomerAddressesAsync(request.CustomerId, cancellationToken);

        // Get customer orders (limited to recent 10)
        var orders = await _unitOfWork.Orders.GetCustomerOrdersAsync(request.CustomerId, cancellationToken);
        var recentOrders = orders.Take(10).ToList();

        // Map to DTO
        var customerDto = _mapper.Map<CustomerDetailsDto>(customer);
        customerDto.Addresses = _mapper.Map<List<AddressDto>>(addresses);
        customerDto.RecentOrders = _mapper.Map<List<OrderSummaryDto>>(recentOrders);

        return Result<CustomerDetailsDto>.Success(customerDto);
    }
}
