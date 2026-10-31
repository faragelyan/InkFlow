namespace InkFlow.Domain;

public class Article
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Author
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    // Navigations
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    public ICollection<ArticleTopic> ArticleTopics { get; set; } = new List<ArticleTopic>();
    public ICollection<ArticleBookmark> ArticleBookmarks { get; set; } = new List<ArticleBookmark>();
    public ICollection<ArticleLike> ArticleLikes { get; set; } = new List<ArticleLike>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
