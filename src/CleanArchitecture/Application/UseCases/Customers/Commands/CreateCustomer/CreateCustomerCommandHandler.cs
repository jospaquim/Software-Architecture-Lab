using AutoMapper;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Customers.Commands.CreateCustomer;

/// <summary>
/// Handler for CreateCustomerCommand
/// Implements Single Responsibility Principle - only handles customer creation
/// Depends on abstractions (IUnitOfWork, IMapper) - Dependency Inversion Principle
/// </summary>
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCustomerCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var emailExists = await _unitOfWork.Customers.EmailExistsAsync(request.Email, cancellationToken: cancellationToken);
        if (emailExists)
        {
            return Result<CustomerDto>.Failure($"Customer with email '{request.Email}' already exists");
        }

        // Create customer entity
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            BirthDate = request.BirthDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database
        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO and return
        var customerDto = _mapper.Map<CustomerDto>(customer);
        return Result<CustomerDto>.Success(customerDto);
    }
}
