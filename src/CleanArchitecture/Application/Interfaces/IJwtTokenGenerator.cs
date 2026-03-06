using CleanArchitecture.Domain.Entities.Auth;

namespace CleanArchitecture.Application.Common;

/// <summary>
/// Interface for JWT token generation
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate an access token for a user
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generate a refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate a token and get the user ID
    /// </summary>
    Guid? ValidateToken(string token);
}
