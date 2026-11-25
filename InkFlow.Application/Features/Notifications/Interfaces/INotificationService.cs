using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Notifications.DTOs.Responses;

namespace InkFlow.Application.Features.Notifications.Interfaces;

public interface INotificationService
{
    Task<Result<PaginationResponse<NotificationResponse>>> GetMineAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<Result> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}