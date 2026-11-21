namespace InkFlow.Application.Features.Auth.DTOs.Responses;

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? ProfileImageUrl,
    IEnumerable<string> Roles
);
