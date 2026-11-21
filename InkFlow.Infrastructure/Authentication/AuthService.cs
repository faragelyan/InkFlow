using System.Security.Claims;
using System.Text;
using FluentValidation;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
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

    // Validators
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<RevokeTokenRequest> _revokeTokenValidator;
    private readonly IValidator<ConfirmEmailRequest> _confirmEmailValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IValidator<GoogleAuthRequest> _googleAuthValidator;

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
        IValidator<RevokeTokenRequest> revokeTokenValidator,
        IValidator<ConfirmEmailRequest> confirmEmailValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<GoogleAuthRequest> googleAuthValidator)
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
        _confirmEmailValidator = confirmEmailValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _changePasswordValidator = changePasswordValidator;
        _googleAuthValidator = googleAuthValidator;
    }

    #region 1. Registration & Login

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthResponse>.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return Result<AuthResponse>.Failure("Email is already registered.");

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
            return Result<AuthResponse>.Failure(createResult.Errors.Select(e => e.Description));

        await EnsureDefaultRoleAsync(user);

        var response = await CreateAuthResponseAsync(user, cancellationToken);

        // Send email confirmation link in background
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));
        var confirmationLink = $"/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";
        _ = _emailService.SendEmailConfirmationEmailAsync(user.Email, confirmationLink, CancellationToken.None);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthResponse>.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || user.IsDeleted || !user.IsActive)
            return Result<AuthResponse>.Failure("Invalid email or password.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var response = await CreateAuthResponseAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    #endregion

    #region 2. Token & Session Management

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _refreshTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthResponse>.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var principal = _jwtProvider.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return Result<AuthResponse>.Failure("Invalid access token.");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Result<AuthResponse>.Failure("Invalid access token claims.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted || !user.IsActive)
            return Result<AuthResponse>.Failure("User account is inactive or not found.");

        var existingRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken);

        if (existingRefreshToken == null || !existingRefreshToken.IsActive)
            return Result<AuthResponse>.Failure("Refresh token is expired or revoked.");

        // Revoke the exchanged refresh token (Rotation)
        existingRefreshToken.RevokedOnUtc = DateTime.UtcNow;

        var response = await CreateAuthResponseAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _revokeTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
            return Result.Failure("Token is already inactive or not found.");

        refreshToken.RevokedOnUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(Guid userId, string? refreshToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId, cancellationToken);

            if (token != null && token.IsActive)
                token.RevokedOnUtc = DateTime.UtcNow;
        }
        else
        {
            // Revoke all active sessions for this user
            await RevokeAllUserTokensAsync(userId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    #endregion

    #region 3. Email Verification & Password Management

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _confirmEmailValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || user.IsDeleted || !user.IsActive)
            return Result.Failure("User not found.");

        if (user.EmailConfirmed)
            return Result.Failure("Email is already confirmed.");

        var token = DecodeToken(request.Token);
        var confirmResult = await _userManager.ConfirmEmailAsync(user, token);

        if (!confirmResult.Succeeded)
        {
            // Fallback to raw token if decoded token failed
            confirmResult = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!confirmResult.Succeeded)
                return Result.Failure(confirmResult.Errors.Select(e => e.Description));
        }

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null && user.IsActive && !user.IsDeleted)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = $"/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

            _ = _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink, CancellationToken.None);
        }

        // Always succeed to prevent user enumeration
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive || user.IsDeleted)
            return Result.Failure("Invalid request.");

        var token = DecodeToken(request.Token);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!resetResult.Succeeded)
                return Result.Failure(resetResult.Errors.Select(e => e.Description));
        }

        // Revoke all active sessions on password reset for security
        await RevokeAllUserTokensAsync(user.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive || user.IsDeleted)
            return Result.Failure("User not found.");

        var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
            return Result.Failure(changeResult.Errors.Select(e => e.Description));

        return Result.Success();
    }

    #endregion

    #region 4. Social / External Authentication

    public async Task<Result<AuthResponse>> GoogleAuthAsync(GoogleAuthRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _googleAuthValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthResponse>.Failure(validation.Errors.Select(e => e.ErrorMessage));

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
        }
        catch (Exception ex)
        {
            return Result<AuthResponse>.Failure($"Google authentication failed: {ex.Message}");
        }

        var user = await _userManager.FindByLoginAsync("Google", payload.Subject)
                   ?? await _userManager.FindByEmailAsync(payload.Email);

        if (user == null)
        {
            // Google Signup
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = payload.Email,
                UserName = payload.Email,
                DisplayName = payload.Name ?? payload.Email,
                ProfileImageUrl = payload.Picture,
                EmailConfirmed = payload.EmailVerified,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Result<AuthResponse>.Failure(createResult.Errors.Select(e => e.Description));

            await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
            await EnsureDefaultRoleAsync(user);
        }
        else
        {
            // Google Login
            if (user.IsDeleted || !user.IsActive)
                return Result<AuthResponse>.Failure("Account is deactivated or deleted.");

            var logins = await _userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == "Google"))
                await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
        }

        var response = await CreateAuthResponseAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    #endregion

    #region 5. Private Helpers

    private async Task<AuthResponse> CreateAuthResponseAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any())
            roles = new[] { "User" };

        var accessToken = _jwtProvider.GenerateAccessToken(user, roles);
        var refreshToken = _jwtProvider.GenerateRefreshToken(user.Id);

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.ProfileImageUrl, roles);
        return new AuthResponse(
            accessToken,
            _jwtOptions.ExpiryInMinutes,
            refreshToken.Token,
            refreshToken.ExpiresOnUtc,
            userDto
        );
    }

    private async Task EnsureDefaultRoleAsync(AppUser user)
    {
        const string defaultRole = "User";
        if (!await _roleManager.RoleExistsAsync(defaultRole))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(defaultRole));

        await _userManager.AddToRoleAsync(user, defaultRole);
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.RevokedOnUtc = DateTime.UtcNow;
    }

    private static string DecodeToken(string rawToken)
    {
        try
        {
            var decodedBytes = WebEncoders.Base64UrlDecode(rawToken);
            return Encoding.UTF8.GetString(decodedBytes);
        }
        catch
        {
            return rawToken;
        }
    }

    #endregion
}
