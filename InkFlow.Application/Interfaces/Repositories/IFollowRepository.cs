namespace InkFlow.Application.Interfaces.Repositories;

using InkFlow.Domain;

public interface IFollowRepository
{
    Task<bool> EnsureFollowingAsync(Guid followerId, Guid authorId, Notification? notification = null, CancellationToken cancellationToken = default);
    Task RemoveFollowingAsync(Guid followerId, Guid authorId, CancellationToken cancellationToken = default);
}