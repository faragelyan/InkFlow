namespace InkFlow.Application.Features.Auth.DTOs.Responses;

public record AuthResponse(
    string AccessToken,
    int ExpiresInMinutes,
    string RefreshToken,
    DateTime RefreshTokenExpiresOnUtc,
    UserDto User
);
