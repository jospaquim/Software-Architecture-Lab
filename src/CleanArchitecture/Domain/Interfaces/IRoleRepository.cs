using CleanArchitecture.Domain.Entities.Auth;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Repository interface for Role entity
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    /// Get role by name
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get roles by names
    /// </summary>
    Task<IEnumerable<Role>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if role exists
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
