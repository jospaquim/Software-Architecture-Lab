using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Commands.Auth;
using CleanArchitecture.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CleanArchitecture.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactoryBase<Program>>
{
    private readonly WebApplicationFactoryBase<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactoryBase<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "SecurePassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange - Register first user
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var firstCommand = new RegisterCommand
        {
            Username = $"user1_{Guid.NewGuid():N}",
            Email = email,
            Password = "SecurePassword123!",
            FirstName = "First",
            LastName = "User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", firstCommand);

        // Act - Try to register second user with same email
        var secondCommand = new RegisterCommand
        {
            Username = $"user2_{Guid.NewGuid():N}",
            Email = email, // Same email
            Password = "SecurePassword123!",
            FirstName = "Second",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", secondCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange - Register a user first
        var username = $"loginuser_{Guid.NewGuid():N}";
        var password = "SecurePassword123!";

        var registerCommand = new RegisterCommand
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = password,
            FirstName = "Login",
            LastName = "Test"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act - Login with the registered credentials
        var loginCommand = new LoginCommand
        {
            Username = username,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange - Register a user
        var username = $"invalidpass_{Guid.NewGuid():N}";
        var correctPassword = "CorrectPassword123!";

        var registerCommand = new RegisterCommand
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = correctPassword,
            FirstName = "Invalid",
            LastName = "Pass"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act - Try to login with wrong password
        var loginCommand = new LoginCommand
        {
            Username = username,
            Password = "WrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginCommand = new LoginCommand
        {
            Username = "nonexistentuser",
            Password = "AnyPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange - Register and login
        var username = $"refreshuser_{Guid.NewGuid():N}";
        var password = "SecurePassword123!";

        var registerCommand = new RegisterCommand
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = password,
            FirstName = "Refresh",
            LastName = "Test"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginCommand = new LoginCommand
        {
            Username = username,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();

        // Act - Refresh the token
        var refreshCommand = new RefreshTokenCommand
        {
            RefreshToken = loginResult!.RefreshToken
        };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().NotBe(loginResult.AccessToken); // New token should be different
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshCommand = new RefreshTokenCommand
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private class LoginResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
