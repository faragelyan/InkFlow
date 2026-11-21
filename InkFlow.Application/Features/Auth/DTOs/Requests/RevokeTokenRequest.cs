namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record RevokeTokenRequest(
    string RefreshToken
);
