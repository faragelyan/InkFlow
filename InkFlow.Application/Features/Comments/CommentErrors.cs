namespace InkFlow.Application.Features.Comments;

public static class CommentErrors
{
    public const string ArticleNotFound = "Published article not found.";
    public const string CommentNotFound = "Comment not found.";
    public const string ParentCommentNotFound = "Parent comment not found for this article.";
    public const string AuthorNotFound = "Active author not found.";
    public const string SaveFailed = "Unable to save comment.";
    public const string UpdateFailed = "Unable to update comment.";
    public const string DeleteFailed = "Unable to delete comment.";
}