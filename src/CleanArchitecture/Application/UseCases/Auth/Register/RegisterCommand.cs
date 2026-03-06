using CleanArchitecture.Application.Common;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Auth.Register;

/// <summary>
/// Command para registrar un nuevo usuario
/// </summary>
public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<Result<RegisterResponse>>;

public record RegisterResponse(
    Guid Uid,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
