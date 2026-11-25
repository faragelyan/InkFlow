namespace InkFlow.Application.Common.Models;

public sealed class ArticleEngagementSnapshot
{
    public int LikeCount { get; init; }
    public int BookmarkCount { get; init; }
    public bool IsLiked { get; init; }
    public bool IsBookmarked { get; init; }
}