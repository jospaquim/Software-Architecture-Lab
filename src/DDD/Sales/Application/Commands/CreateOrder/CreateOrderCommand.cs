using DDD.Sales.Domain.ValueObjects;
using MediatR;

namespace DDD.Sales.Application.Commands.CreateOrder;

/// <summary>
/// Command to create a new order - follows DDD command pattern
/// </summary>
public record CreateOrderCommand(
    Guid CustomerId,
    string Street,
    string City,
    string State,
    string Country,
    string ZipCode) : IRequest<Result<Guid>>;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }

    private Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}
