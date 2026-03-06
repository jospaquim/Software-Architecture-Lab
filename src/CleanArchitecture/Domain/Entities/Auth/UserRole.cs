namespace CleanArchitecture.Domain.Entities.Auth;

/// <summary>
/// Many-to-many relationship between User and Role
/// </summary>
public class UserRole
{
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public DateTime AssignedAt { get; private set; }

    private UserRole() { } // Para EF Core

    private UserRole(int userId, int roleId)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
    }

    public static UserRole Create(int userId, int roleId)
    {
        return new UserRole(userId, roleId);
    }
}
