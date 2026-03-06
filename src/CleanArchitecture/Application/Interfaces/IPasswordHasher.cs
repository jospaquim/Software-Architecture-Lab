namespace CleanArchitecture.Application.Common;

/// <summary>
/// Interface for password hashing
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
