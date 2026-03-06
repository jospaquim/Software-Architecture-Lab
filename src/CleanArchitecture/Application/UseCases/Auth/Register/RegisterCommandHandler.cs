using CleanArchitecture.Application.Common;
using CleanArchitecture.Domain.Entities.Auth;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.UseCases.Auth.Register;

/// <summary>
/// Handler para registrar un nuevo usuario
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user: {Username}", request.Username);

        // Verificar si el username ya existe
        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            _logger.LogWarning("Username already exists: {Username}", request.Username);
            return Result<RegisterResponse>.Failure("Username already exists");
        }

        // Verificar si el email ya existe
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            _logger.LogWarning("Email already exists: {Email}", request.Email);
            return Result<RegisterResponse>.Failure("Email already exists");
        }

        try
        {
            // Hash del password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // Crear el usuario
            var user = User.Create(
                request.Username,
                request.Email,
                passwordHash,
                request.FirstName,
                request.LastName
            );

            // Asignar rol por defecto (User)
            var defaultRole = await _roleRepository.GetByNameAsync(Role.DefaultRoles.User, cancellationToken);
            if (defaultRole != null)
            {
                user.AddRole(defaultRole);
            }

            // Guardar el usuario
            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User registered successfully: {UserId}", user.Id);

            // Generar tokens
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Guardar refresh token
            user.SetRefreshToken(refreshToken, refreshTokenExpiry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new RegisterResponse(
                user.Uid,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                accessToken,
                refreshToken,
                refreshTokenExpiry
            );

            return Result<RegisterResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", request.Username);
            return Result<RegisterResponse>.Failure($"Error registering user: {ex.Message}");
        }
    }
}
