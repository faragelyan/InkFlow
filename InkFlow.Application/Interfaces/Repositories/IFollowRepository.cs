namespace InkFlow.Application.Interfaces.Repositories;

public interface IFollowRepository
{
    Task EnsureFollowingAsync(Guid followerId, Guid authorId, CancellationToken cancellationToken = default);
    Task RemoveFollowingAsync(Guid followerId, Guid authorId, CancellationToken cancellationToken = default);
}