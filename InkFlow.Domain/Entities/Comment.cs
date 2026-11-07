namespace InkFlow.Domain;

public class Comment
{
    public Guid CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Author
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    // Target Article
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    // Self-referencing (Threaded replies)
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    // Engagements & Moderation
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
