using CleanArchitecture.Domain.Entities.Auth;
using FluentAssertions;
using Xunit;

namespace CleanArchitecture.Domain.Tests.Entities.Auth;

/// <summary>
/// Tests unitarios para la entidad User
/// Validan autenticación, autorización y gestión de roles
/// </summary>
public class UserTests
{
    [Fact]
    public void Create_ShouldCreateUserWithCorrectProperties()
    {
        // Arrange
        var username = "johndoe";
        var email = "john@example.com";
        var passwordHash = "hash123";
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var user = User.Create(username, email, passwordHash, firstName, lastName);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.IsActive.Should().BeTrue();
        user.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyUsername_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => User.Create("", "email@test.com", "hash", "First", "Last");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Username cannot be empty*");
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => User.Create("user", "invalid-email", "hash", "First", "Last");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void ConfirmEmail_ShouldSetEmailConfirmedToTrue()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");

        // Act
        user.ConfirmEmail();

        // Assert
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetRefreshToken_ShouldSetTokenAndExpiry()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        var token = "refresh_token_123";
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        user.SetRefreshToken(token, expiry);

        // Assert
        user.RefreshToken.Should().Be(token);
        user.RefreshTokenExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public void SetRefreshToken_WithPastExpiry_ShouldThrowArgumentException()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        Action act = () => user.SetRefreshToken("token", pastDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Expiration date must be in the future*");
    }

    [Fact]
    public void IsRefreshTokenValid_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        var token = "refresh_token_123";
        var expiry = DateTime.UtcNow.AddDays(7);
        user.SetRefreshToken(token, expiry);

        // Act
        var isValid = user.IsRefreshTokenValid(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenValid_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        var token = "refresh_token_123";
        var expiry = DateTime.UtcNow.AddSeconds(1); // Expira en 1 segundo
        user.SetRefreshToken(token, expiry);

        System.Threading.Thread.Sleep(1100); // Esperar que expire

        // Act
        var isValid = user.IsRefreshTokenValid(token);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenValid_WithWrongToken_ShouldReturnFalse()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        user.SetRefreshToken("correct_token", DateTime.UtcNow.AddDays(7));

        // Act
        var isValid = user.IsRefreshTokenValid("wrong_token");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void RevokeRefreshToken_ShouldClearTokenAndExpiry()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));

        // Act
        user.RevokeRefreshToken();

        // Assert
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void AddRole_ShouldAddRoleToUser()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        var role = Role.Create("Admin", "Administrator role");

        // Act
        user.AddRole(role);

        // Assert
        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().RoleId.Should().Be(role.Id);
    }

    [Fact]
    public void AddRole_WhenAlreadyHasRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "First", "Last");
        var role = Role.Create("Admin", "Administrator role");
        user.AddRole(role);

        // Act
        Action act = () => user.AddRole(role);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already has role*");
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateFirstNameAndLastName()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "hash", "Old First", "Old Last");

        // Act
        user.UpdateProfile("New First", "New Last");

        // Assert
        user.FirstName.Should().Be("New First");
        user.LastName.Should().Be("New Last");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangePassword_ShouldUpdatePasswordHash()
    {
        // Arrange
        var user = User.Create("user", "test@example.com", "old_hash", "First", "Last");

        // Act
        user.ChangePassword("new_hash");

        // Assert
        user.PasswordHash.Should().Be("new_hash");
        user.UpdatedAt.Should().NotBeNull();
    }
}
