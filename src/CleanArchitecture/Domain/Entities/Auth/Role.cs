using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities.Auth;

/// <summary>
/// Represents a user role
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    // Navigation property
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() { } // Para EF Core

    private Role(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Factory method para crear un nuevo rol
    /// </summary>
    public static Role Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Role description cannot be empty", nameof(description));

        return new Role(name, description);
    }

    /// <summary>
    /// Actualizar rol
    /// </summary>
    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Role description cannot be empty", nameof(description));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    // Roles predefinidos
    public static class DefaultRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Manager = "Manager";
    }
}
