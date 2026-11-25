using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;
using InkFlow.Infrastructure.Persistence.Context;

namespace InkFlow.Infrastructure.Persistence.Repositories;

public sealed class CommentEngagementRepository : ICommentEngagementRepository
{
    private readonly AppDbContext _context;

    public CommentEngagementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<Guid, CommentEngagementSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> commentIds,
        Guid? viewerId,
        CancellationToken cancellationToken = default)
    {
        if (commentIds.Count == 0)
            return new Dictionary<Guid, CommentEngagementSnapshot>();

        var ids = commentIds.Distinct().ToArray();
        var counts = await _context.CommentLikes
            .AsNoTracking()
            .Where(like => ids.Contains(like.CommentId))
            .GroupBy(like => like.CommentId)
            .Select(group => new { CommentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CommentId, item => item.Count, cancellationToken);
        var likedIds = viewerId is Guid currentUserId
            ? await _context.CommentLikes.AsNoTracking()
                .Where(like => like.UserId == currentUserId && ids.Contains(like.CommentId))
                .Select(like => like.CommentId)
                .ToHashSetAsync(cancellationToken)
            : new HashSet<Guid>();
        var replyCounts = await _context.Comments
            .AsNoTracking()
            .Where(comment => comment.ParentCommentId.HasValue && ids.Contains(comment.ParentCommentId.Value))
            .GroupBy(comment => comment.ParentCommentId!.Value)
            .Select(group => new { CommentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CommentId, item => item.Count, cancellationToken);

        return ids.ToDictionary(
            commentId => commentId,
            commentId => new CommentEngagementSnapshot
            {
                LikeCount = counts.GetValueOrDefault(commentId),
                ReplyCount = replyCounts.GetValueOrDefault(commentId),
                IsLiked = likedIds.Contains(commentId)
            });
    }

    public async Task<bool> EnsureLikeAsync(
        Guid commentId,
        Guid userId,
        Notification? notification = null,
        CancellationToken cancellationToken = default)
    {
        var likes = _context.CommentLikes;
        if (await likes.AnyAsync(like => like.CommentId == commentId && like.UserId == userId, cancellationToken))
            return false;

        var likeEntry = await likes.AddAsync(new CommentLike { CommentId = commentId, UserId = userId }, cancellationToken);
        EntityEntry<Notification>? notificationEntry = null;
        if (notification is not null)
            notificationEntry = await _context.Notifications.AddAsync(notification, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsDuplicateKeyViolation(exception))
        {
            likeEntry.State = EntityState.Detached;
            if (notificationEntry is not null)
                notificationEntry.State = EntityState.Detached;

            if (await likes.AsNoTracking().AnyAsync(
                    like => like.CommentId == commentId && like.UserId == userId,
                    cancellationToken))
                return false;

            throw;
        }
    }

    public async Task RemoveLikeAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.CommentLikes
            .Where(like => like.CommentId == commentId && like.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    }
}