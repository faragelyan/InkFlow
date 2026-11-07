namespace InkFlow.Domain;

public class Notification
{
    public Guid NotificationId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Recipient User
    public Guid ReceiverUserId { get; set; }
    public AppUser Receiver { get; set; } = null!;

    // Actor User (Triggered by)
    public Guid ActorUserId { get; set; }
    public AppUser Actor { get; set; } = null!;

    // Optional Targets
    public Guid? ArticleId { get; set; }
    public Article? Article { get; set; }

    public Guid? CommentId { get; set; }
    public Comment? Comment { get; set; }
}
