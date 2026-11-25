using InkFlow.Application.Common.Models;
using InkFlow.Domain;

namespace InkFlow.Application.Interfaces.Repositories;

public interface ICommentEngagementRepository
{
    Task<IReadOnlyDictionary<Guid, CommentEngagementSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> commentIds,
        Guid? viewerId,
        CancellationToken cancellationToken = default);

    Task<bool> EnsureLikeAsync(
        Guid commentId,
        Guid userId,
        Notification? notification = null,
        CancellationToken cancellationToken = default);

    Task RemoveLikeAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default);
}