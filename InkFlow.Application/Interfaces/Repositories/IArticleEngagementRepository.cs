using InkFlow.Application.Common.Models;

namespace InkFlow.Application.Interfaces.Repositories;

public interface IArticleEngagementRepository
{
    Task<IReadOnlyDictionary<Guid, ArticleEngagementSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> articleIds,
        Guid? viewerId,
        CancellationToken cancellationToken = default);

    Task EnsureLikeAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default);
    Task RemoveLikeAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default);
    Task EnsureBookmarkAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default);
    Task RemoveBookmarkAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default);
}