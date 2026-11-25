using InkFlow.Domain;

namespace InkFlow.Application.Features.Notifications.DTOs.Responses;

public class NotificationResponse
{
    public Guid NotificationId { get; init; }
    public NotificationType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public string ActorDisplayName { get; init; } = string.Empty;
    public string? ActorProfileImageUrl { get; init; }
    public Guid? ArticleId { get; init; }
    public Guid? CommentId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReadAt { get; init; }
}