namespace InkFlow.Application.Features.Articles.DTOs.Responses;

public class ArticleEngagementResponse
{
    public Guid ArticleId { get; init; }
    public int LikeCount { get; init; }
    public int BookmarkCount { get; init; }
    public bool IsLiked { get; init; }
    public bool IsBookmarked { get; init; }
}