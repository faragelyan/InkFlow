using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;
using InkFlow.Infrastructure.Persistence.Context;

namespace InkFlow.Infrastructure.Persistence.Repositories;

public sealed class ArticleEngagementRepository : IArticleEngagementRepository
{
    private readonly AppDbContext _context;

    public ArticleEngagementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<Guid, ArticleEngagementSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> articleIds,
        Guid? viewerId,
        CancellationToken cancellationToken = default)
    {
        if (articleIds.Count == 0)
            return new Dictionary<Guid, ArticleEngagementSnapshot>();

        var ids = articleIds.Distinct().ToArray();
        var likeCounts = await _context.ArticleLikes
            .AsNoTracking()
            .Where(like => ids.Contains(like.ArticleId))
            .GroupBy(like => like.ArticleId)
            .Select(group => new { ArticleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ArticleId, item => item.Count, cancellationToken);
        var bookmarkCounts = await _context.ArticleBookmarks
            .AsNoTracking()
            .Where(bookmark => ids.Contains(bookmark.ArticleId))
            .GroupBy(bookmark => bookmark.ArticleId)
            .Select(group => new { ArticleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ArticleId, item => item.Count, cancellationToken);

        var likedArticleIds = viewerId is Guid likeViewerId
            ? await _context.ArticleLikes.AsNoTracking()
                .Where(like => like.UserId == likeViewerId && ids.Contains(like.ArticleId))
                .Select(like => like.ArticleId)
                .ToHashSetAsync(cancellationToken)
            : new HashSet<Guid>();
        var bookmarkedArticleIds = viewerId is Guid bookmarkViewerId
            ? await _context.ArticleBookmarks.AsNoTracking()
                .Where(bookmark => bookmark.UserId == bookmarkViewerId && ids.Contains(bookmark.ArticleId))
                .Select(bookmark => bookmark.ArticleId)
                .ToHashSetAsync(cancellationToken)
            : new HashSet<Guid>();

        return ids.ToDictionary(
            articleId => articleId,
            articleId => new ArticleEngagementSnapshot
            {
                LikeCount = likeCounts.GetValueOrDefault(articleId),
                BookmarkCount = bookmarkCounts.GetValueOrDefault(articleId),
                IsLiked = likedArticleIds.Contains(articleId),
                IsBookmarked = bookmarkedArticleIds.Contains(articleId)
            });
    }

    public Task EnsureLikeAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default)
    {
        return EnsureExistsAsync(
            _context.ArticleLikes,
            new ArticleLike { ArticleId = articleId, UserId = userId },
            like => like.ArticleId == articleId && like.UserId == userId,
            cancellationToken);
    }

    public async Task RemoveLikeAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.ArticleLikes
            .Where(like => like.ArticleId == articleId && like.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task EnsureBookmarkAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default)
    {
        return EnsureExistsAsync(
            _context.ArticleBookmarks,
            new ArticleBookmark { ArticleId = articleId, UserId = userId },
            bookmark => bookmark.ArticleId == articleId && bookmark.UserId == userId,
            cancellationToken);
    }

    public async Task RemoveBookmarkAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.ArticleBookmarks
            .Where(bookmark => bookmark.ArticleId == articleId && bookmark.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task EnsureExistsAsync<TEntity>(
        DbSet<TEntity> set,
        TEntity entity,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (await set.AnyAsync(predicate, cancellationToken))
            return;

        var entry = await set.AddAsync(entity, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKeyViolation(exception))
        {
            entry.State = EntityState.Detached;
            if (await set.AsNoTracking().AnyAsync(predicate, cancellationToken))
                return;

            throw;
        }
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    }
}