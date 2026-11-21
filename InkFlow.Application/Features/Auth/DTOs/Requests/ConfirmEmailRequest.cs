namespace InkFlow.Application.Features.Auth.DTOs.Requests;

public record ConfirmEmailRequest(
    string UserId,
    string Token
);
