namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword
);
