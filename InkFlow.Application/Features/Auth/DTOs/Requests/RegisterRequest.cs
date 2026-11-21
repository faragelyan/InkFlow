namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName
);
