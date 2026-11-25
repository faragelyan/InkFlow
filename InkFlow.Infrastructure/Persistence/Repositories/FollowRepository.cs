using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Data.SqlClient;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;
using InkFlow.Infrastructure.Persistence.Context;

namespace InkFlow.Infrastructure.Persistence.Repositories;

public sealed class FollowRepository : IFollowRepository
{
    private readonly AppDbContext _context;

    public FollowRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureFollowingAsync(Guid followerId, Guid authorId, CancellationToken cancellationToken = default)
    {
        var follows = _context.Follows;
        if (await follows.AnyAsync(
                follow => follow.FollowerId == followerId && follow.FollowingId == authorId,
                cancellationToken))
            return;

        var entry = await follows.AddAsync(new Follow
        {
            FollowerId = followerId,
            FollowingId = authorId
        }, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKeyViolation(exception))
        {
            entry.State = EntityState.Detached;
            if (await follows.AsNoTracking().AnyAsync(
                    follow => follow.FollowerId == followerId && follow.FollowingId == authorId,
                    cancellationToken))
                return;

            throw;
        }
    }

    public async Task RemoveFollowingAsync(Guid followerId, Guid authorId, CancellationToken cancellationToken = default)
    {
        await _context.Follows
            .Where(follow => follow.FollowerId == followerId && follow.FollowingId == authorId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    }
}