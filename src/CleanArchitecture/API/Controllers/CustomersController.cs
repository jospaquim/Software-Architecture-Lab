using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Customers.Commands.CreateCustomer;
using CleanArchitecture.Application.UseCases.Customers.Queries.GetCustomerById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CleanArchitecture.API.Controllers;

/// <summary>
/// Customers API Controller
/// Manages customer operations
/// </summary>
[Authorize] // Requires authentication
[EnableRateLimiting("sliding")]
public class CustomersController : BaseApiController
{
    /// <summary>
    /// Get customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var query = new GetCustomerByIdQuery(id);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    /// <param name="dto">Customer creation data</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        var command = new CreateCustomerCommand(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.Phone,
            dto.BirthDate);

        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = result.Value!.Id },
                result.Value);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get all customers (paginated)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>List of customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // TODO: Implement GetCustomersQuery
        // For now, return empty list
        return Ok(new List<CustomerDto>());
    }

    /// <summary>
    /// Update customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated customer</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("ID mismatch");
        }

        // TODO: Implement UpdateCustomerCommand
        return NoContent();
    }

    /// <summary>
    /// Delete customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        // TODO: Implement DeleteCustomerCommand
        return NoContent();
    }

    /// <summary>
    /// Deactivate customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateCustomer(int id)
    {
        // TODO: Implement DeactivateCustomerCommand
        return NoContent();
    }

    /// <summary>
    /// Promote customer to VIP
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpPost("{id}/promote-to-vip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PromoteToVip(int id)
    {
        // TODO: Implement PromoteCustomerToVipCommand
        return NoContent();
    }
}
