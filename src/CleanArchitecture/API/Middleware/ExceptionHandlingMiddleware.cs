using System.Net;
using System.Text.Json;
using FluentValidation;

namespace CleanArchitecture.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// Catches all unhandled exceptions and returns appropriate HTTP responses
/// Follows Clean Code principles - centralized error handling
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred",
                Errors = validationException.Errors.Select(e => new ErrorDetail
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList()
            },

            ArgumentNullException _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            },

            ArgumentException _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            },

            InvalidOperationException _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Operation",
                Detail = exception.Message
            },

            UnauthorizedAccessException _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = "You are not authorized to access this resource"
            },

            KeyNotFoundException _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = exception.Message
            },

            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.",
                StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
            }
        };

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Standard error response following RFC 7807 Problem Details
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
    public string? StackTrace { get; set; }
    public List<ErrorDetail>? Errors { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
