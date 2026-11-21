using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Auth.DTOs.Requests;
using InkFlow.Application.Features.Auth.DTOs.Responses;

namespace InkFlow.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);
}
