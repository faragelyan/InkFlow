using InkFlow.Application.Features.Notifications.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Notifications.Mappings;

public static class NotificationMappingConfig
{
    public static NotificationResponse ToResponse(Notification notification, AppUser? actor)
    {
        return new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            Type = notification.NotificationType,
            Content = notification.Content,
            ActorUserId = notification.ActorUserId,
            ActorDisplayName = actor is { IsActive: true, IsDeleted: false } ? actor.DisplayName : "Inactive user",
            ActorProfileImageUrl = actor is { IsActive: true, IsDeleted: false } ? actor.ProfileImageUrl : null,
            ArticleId = notification.ArticleId,
            CommentId = notification.CommentId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}