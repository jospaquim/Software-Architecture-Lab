using CleanArchitecture.Application.Common;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Auth.Login;

/// <summary>
/// Command para login de usuario
/// </summary>
public record LoginCommand(
    string UsernameOrEmail,
    string Password
) : IRequest<Result<LoginResponse>>;

public record LoginResponse(
    Guid UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    IEnumerable<string> Roles
);
