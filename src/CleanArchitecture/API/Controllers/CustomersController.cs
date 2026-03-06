using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Customers.Commands.CreateCustomer;
using CleanArchitecture.Application.UseCases.Customers.Queries.GetCustomerById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CleanArchitecture.API.Controllers;

[Authorize]
[EnableRateLimiting("sliding")]
public class CustomersController : BaseApiController
{
    [HttpGet("{uid:guid}")]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomer(Guid uid)
    {
        var query = new GetCustomerByIdQuery(uid);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

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
                new { uid = result.Value!.Uid },
                result.Value);
        }

        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // TODO: Implement GetCustomersQuery
        return Ok(new List<CustomerDto>());
    }

    [HttpPut("{uid:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCustomer(Guid uid, [FromBody] UpdateCustomerDto dto)
    {
        if (uid != dto.Uid)
        {
            return BadRequest("UID mismatch");
        }

        // TODO: Implement UpdateCustomerCommand
        return NoContent();
    }

    [HttpDelete("{uid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCustomer(Guid uid)
    {
        // TODO: Implement DeleteCustomerCommand
        return NoContent();
    }

    [HttpPost("{uid:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateCustomer(Guid uid)
    {
        // TODO: Implement DeactivateCustomerCommand
        return NoContent();
    }

    [HttpPost("{uid:guid}/promote-to-vip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PromoteToVip(Guid uid)
    {
        // TODO: Implement PromoteCustomerToVipCommand
        return NoContent();
    }
}
