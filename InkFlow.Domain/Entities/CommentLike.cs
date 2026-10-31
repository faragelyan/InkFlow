namespace InkFlow.Domain;

public class CommentLike
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
