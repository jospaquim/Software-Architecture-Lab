using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Customers.Commands.CreateCustomer;

/// <summary>
/// Command to create a new customer
/// Follows CQRS pattern - Command is a write operation
/// </summary>
public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime? BirthDate) : IRequest<Result<CustomerDto>>;
