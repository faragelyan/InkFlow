namespace InkFlow.Application.Features.Authors.DTOs.Responses;

public class AuthorProfileResponse
{
    public Guid AuthorId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? ProfileImageUrl { get; init; }
    public DateTime JoinedAt { get; init; }
    public int PublishedArticleCount { get; init; }
    public int FollowerCount { get; init; }
    public int FollowingCount { get; init; }
    public bool IsFollowing { get; init; }
}