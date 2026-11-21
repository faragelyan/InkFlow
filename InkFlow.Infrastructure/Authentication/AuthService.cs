using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Auth.DTOs.Requests;
using InkFlow.Application.Features.Auth.DTOs.Responses;
using InkFlow.Application.Features.Auth.Interfaces;
using InkFlow.Application.Interfaces.ExternalServices;
using InkFlow.Domain;
using InkFlow.Infrastructure.Persistence.Context;

namespace InkFlow.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IJwtProvider _jwtProvider;
    private readonly IEmailService _emailService;
    private readonly AppDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<RevokeTokenRequest> _revokeTokenValidator;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IJwtProvider jwtProvider,
        IEmailService emailService,
        AppDbContext context,
        IOptions<JwtOptions> jwtOptions,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<RevokeTokenRequest> revokeTokenValidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtProvider = jwtProvider;
        _emailService = emailService;
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _revokeTokenValidator = revokeTokenValidator;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuthResponse>.Failure(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("Email is already registered.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description);
            return Result<AuthResponse>.Failure(errors);
        }

        // Ensure default role
        const string defaultRole = "User";
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(defaultRole));
        }
        await _userManager.AddToRoleAsync(user, defaultRole);

        var roles = new[] { defaultRole };
        var accessToken = _jwtProvider.GenerateAccessToken(user, roles);
        var refreshToken = _jwtProvider.GenerateRefreshToken(user.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Background welcome email (not cancelled by request abort)
        _ = _emailService.SendWelcomeEmailAsync(user.Email, user.DisplayName, CancellationToken.None);

        var userDto = new UserDto(user.Id, user.Email, user.DisplayName, user.ProfileImageUrl, roles);
        var response = new AuthResponse(
            accessToken,
            _jwtOptions.ExpiryInMinutes,
            refreshToken.Token,
            refreshToken.ExpiresOnUtc,
            userDto
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuthResponse>.Failure(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || user.IsDeleted || !user.IsActive)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtProvider.GenerateAccessToken(user, roles);
        var refreshToken = _jwtProvider.GenerateRefreshToken(user.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.ProfileImageUrl, roles);
        var response = new AuthResponse(
            accessToken,
            _jwtOptions.ExpiryInMinutes,
            refreshToken.Token,
            refreshToken.ExpiresOnUtc,
            userDto
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _refreshTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AuthResponse>.Failure(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var principal = _jwtProvider.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return Result<AuthResponse>.Failure("Invalid access token.");
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Result<AuthResponse>.Failure("Invalid access token claims.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted || !user.IsActive)
        {
            return Result<AuthResponse>.Failure("User account is inactive or not found.");
        }

        var existingRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken);

        if (existingRefreshToken == null || !existingRefreshToken.IsActive)
        {
            return Result<AuthResponse>.Failure("Refresh token is expired or revoked.");
        }

        // Revoke the old refresh token
        existingRefreshToken.RevokedOnUtc = DateTime.UtcNow;

        // Generate new token pair
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtProvider.GenerateAccessToken(user, roles);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken(user.Id);

        await _context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.ProfileImageUrl, roles);
        var response = new AuthResponse(
            newAccessToken,
            _jwtOptions.ExpiryInMinutes,
            newRefreshToken.Token,
            newRefreshToken.ExpiresOnUtc,
            userDto
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _revokeTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result.Failure("Token is already inactive or not found.");
        }

        refreshToken.RevokedOnUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
