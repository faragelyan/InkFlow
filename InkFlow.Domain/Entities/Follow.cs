namespace InkFlow.Domain;

public class Follow
{
    public Guid FollowerId { get; set; }
    public AppUser Follower { get; set; } = null!;

    public Guid FollowingId { get; set; }
    public AppUser Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
