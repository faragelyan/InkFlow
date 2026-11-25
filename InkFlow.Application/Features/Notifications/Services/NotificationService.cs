using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Notifications.DTOs.Responses;
using InkFlow.Application.Features.Notifications.Interfaces;
using InkFlow.Application.Features.Notifications.Mappings;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Notification> _notifications;
    private readonly IGenericRepository<AppUser> _users;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _notifications = unitOfWork.GetRepository<Notification>();
        _users = unitOfWork.GetRepository<AppUser>();
    }

    public async Task<Result<PaginationResponse<NotificationResponse>>> GetMineAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var page = await _notifications.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: notification => notification.ReceiverUserId == userId,
            orderBy: query => query.OrderByDescending(notification => notification.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var actorIds = page.Items.Select(notification => notification.ActorUserId).Distinct().ToList();
        var actors = actorIds.Count == 0
            ? []
            : await _users.GetAllAsync(actor => actorIds.Contains(actor.Id), disableTracking: true, cancellationToken: cancellationToken);
        var actorLookup = actors.ToDictionary(actor => actor.Id);
        var items = page.Items
            .Select(notification => NotificationMappingConfig.ToResponse(
                notification,
                actorLookup.GetValueOrDefault(notification.ActorUserId)))
            .ToList();

        return Result<PaginationResponse<NotificationResponse>>.Success(
            new PaginationResponse<NotificationResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Result> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetSingleAsync(
            item => item.NotificationId == notificationId && item.ReceiverUserId == userId,
            cancellationToken: cancellationToken);
        if (notification is null)
            return Result.Failure(NotificationErrors.NotFound);

        if (notification.IsRead)
            return Result.Success();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        if (!await _notifications.UpdateAsync(notification, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result.Failure(NotificationErrors.UpdateFailed);

        return Result.Success();
    }

    public async Task<Result> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _notifications.GetAllAsync(
            notification => notification.ReceiverUserId == userId && !notification.IsRead,
            cancellationToken: cancellationToken);
        if (unread.Count == 0)
            return Result.Success();

        var readAt = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = readAt;
        }

        if (!await _notifications.UpdateRangeAsync(unread, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result.Failure(NotificationErrors.UpdateFailed);

        return Result.Success();
    }
}