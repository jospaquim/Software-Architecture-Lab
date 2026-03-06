using CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.API.Controllers;

/// <summary>
/// Base API Controller
/// Provides common functionality for all controllers
/// Follows DRY principle
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Helper method to handle Result pattern responses
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Errors.Any())
        {
            return BadRequest(new
            {
                errors = result.Errors
            });
        }

        return BadRequest(new
        {
            error = result.Error
        });
    }

    /// <summary>
    /// Helper method to handle Result pattern responses without value
    /// </summary>
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        if (result.Errors.Any())
        {
            return BadRequest(new
            {
                errors = result.Errors
            });
        }

        return BadRequest(new
        {
            error = result.Error
        });
    }

    /// <summary>
    /// Helper method to handle paged results
    /// </summary>
    protected IActionResult HandlePagedResult<T>(PagedResult<T> result)
    {
        var metadata = new
        {
            result.TotalCount,
            result.PageSize,
            result.PageNumber,
            result.TotalPages,
            result.HasNextPage,
            result.HasPreviousPage
        };

        Response.Headers.Add("X-Pagination", System.Text.Json.JsonSerializer.Serialize(metadata));

        return Ok(result.Items);
    }
}
