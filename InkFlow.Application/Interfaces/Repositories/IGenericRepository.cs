using System.Linq.Expressions;
using InkFlow.Application.Common.Models;

namespace InkFlow.Application.Interfaces.Repositories;

public interface IGenericRepository<TEntity> where TEntity : class
{
    // --- Mutations ---
    Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;
    Task<bool> DeleteRangeByIdAsync<TId>(IEnumerable<TId> ids, CancellationToken cancellationToken = default) where TId : notnull;
    Task<bool> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    // --- Queries ---

    // 1. Get multiple entities
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        bool splitQuery = false,
        CancellationToken cancellationToken = default
    );

    // 2. Get single entity by condition
    Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        bool splitQuery = false,
        CancellationToken cancellationToken = default
    );

    // 3. Get entity by Primary Key
    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;

    // 4. Get paginated entities
    Task<PaginationResponse<TEntity>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        bool splitQuery = false,
        CancellationToken cancellationToken = default
    );

    // 5. Get selected properties (Projection)
    Task<IReadOnlyList<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selectExpression,
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool distinct = false,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default
    );

    // 6. Get entities count
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default
    );

    // 7. Check if any entity exists matching condition
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default
    );

    // 8. Get entities as Dictionary
    Task<Dictionary<TKey, TResult>> GetDictionaryAsync<TKey, TResult>(
        Expression<Func<TEntity, TKey>> keySelector,
        Expression<Func<TEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;
}