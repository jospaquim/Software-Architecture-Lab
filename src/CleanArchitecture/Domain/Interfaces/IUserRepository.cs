using CleanArchitecture.Domain.Entities.Auth;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get user by username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user with roles
    /// </summary>
    Task<User?> GetWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if username exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by refresh token
    /// </summary>
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
