namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);
