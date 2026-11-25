namespace InkFlow.Application.Features.Comments.DTOs.Responses;

public class CommentResponse
{
    public Guid CommentId { get; init; }
    public Guid ArticleId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string? Content { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int ReplyCount { get; init; }
}