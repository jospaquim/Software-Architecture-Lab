using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Customers.Queries.GetCustomerById;

/// <summary>
/// Query to get a customer by ID
/// Follows CQRS pattern - Query is a read-only operation
/// </summary>
public record GetCustomerByIdQuery(int CustomerId) : IRequest<Result<CustomerDetailsDto>>;
