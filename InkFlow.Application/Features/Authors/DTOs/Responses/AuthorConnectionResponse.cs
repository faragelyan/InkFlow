namespace InkFlow.Application.Features.Authors.DTOs.Responses;

public class AuthorConnectionResponse
{
    public Guid AuthorId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public DateTime FollowedAt { get; init; }
}