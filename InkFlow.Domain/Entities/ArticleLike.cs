namespace InkFlow.Domain;

public class ArticleLike
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
