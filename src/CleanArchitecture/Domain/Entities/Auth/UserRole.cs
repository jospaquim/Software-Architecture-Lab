namespace CleanArchitecture.Domain.Entities.Auth;

/// <summary>
/// Many-to-many relationship between User and Role
/// </summary>
public class UserRole
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public DateTime AssignedAt { get; private set; }

    private UserRole() { } // Para EF Core

    private UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
    }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException("RoleId cannot be empty", nameof(roleId));

        return new UserRole(userId, roleId);
    }
}
