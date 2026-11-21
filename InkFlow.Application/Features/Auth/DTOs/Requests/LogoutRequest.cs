namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record LogoutRequest(
    string? RefreshToken = null
);
