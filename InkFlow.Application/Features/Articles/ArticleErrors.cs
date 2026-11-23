namespace InkFlow.Application.Features.Articles;

public static class ArticleErrors
{
    public const string AuthorNotFound = "Author not found.";
    public const string ArticleNotFound = "Article not found.";
    public const string ArticleUnavailable = "Article is not available for this operation.";
    public const string CannotPublishArchived = "Archived articles cannot be published.";
    public const string CannotUnpublishArchived = "Archived articles cannot be unpublished.";
    public const string UpdateFailed = "Unable to update article.";
    public const string SaveFailed = "Unable to save article.";
}