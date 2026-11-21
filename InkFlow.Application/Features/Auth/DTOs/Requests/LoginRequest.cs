namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record LoginRequest(
    string Email,
    string Password
);
