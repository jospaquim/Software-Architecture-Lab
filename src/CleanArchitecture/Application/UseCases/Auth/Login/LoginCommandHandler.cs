using CleanArchitecture.Application.Common;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Auth.Login;

/// <summary>
/// Handler para login de usuario
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for: {UsernameOrEmail}", request.UsernameOrEmail);

        // Buscar usuario por username o email
        var user = await _userRepository.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(request.UsernameOrEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found: {UsernameOrEmail}", request.UsernameOrEmail);
            return Result<LoginResponse>.Failure("Invalid credentials");
        }

        // Verificar que el usuario esté activo
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User is inactive: {UserId}", user.Id);
            return Result<LoginResponse>.Failure("User account is inactive");
        }

        // Verificar password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user: {UserId}", user.Id);
            return Result<LoginResponse>.Failure("Invalid credentials");
        }

        try
        {
            // Cargar roles
            user = await _userRepository.GetWithRolesAsync(user.Id, cancellationToken);
            if (user == null)
            {
                return Result<LoginResponse>.Failure("User not found");
            }

            // Actualizar último login
            user.UpdateLastLogin();

            // Generar tokens
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Guardar refresh token
            user.SetRefreshToken(refreshToken, refreshTokenExpiry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Login successful for user: {UserId}", user.Id);

            var response = new LoginResponse(
                user.Uid,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                accessToken,
                refreshToken,
                refreshTokenExpiry,
                user.UserRoles.Select(ur => ur.Role.Name).ToList()
            );

            return Result<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {UserId}", user.Id);
            return Result<LoginResponse>.Failure($"Error during login: {ex.Message}");
        }
    }
}
