using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Auth.DTOs.Requests;
using InkFlow.Application.Features.Auth.DTOs.Responses;

namespace InkFlow.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    // --- 1. Registration & Login ---
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    // --- 2. Token & Session Management ---
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(Guid userId, string? refreshToken = null, CancellationToken cancellationToken = default);

    // --- 3. Email Verification & Password Management ---
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    // --- 4. Social / External Authentication ---
    Task<Result<AuthResponse>> GoogleAuthAsync(GoogleAuthRequest request, CancellationToken cancellationToken = default);
}
