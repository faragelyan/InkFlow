namespace InkFlow.Application.Common.Models;

public sealed class CommentEngagementSnapshot
{
    public int LikeCount { get; init; }
    public int ReplyCount { get; init; }
    public bool IsLiked { get; init; }
}