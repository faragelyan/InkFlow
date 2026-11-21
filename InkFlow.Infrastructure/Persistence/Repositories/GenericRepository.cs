using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Infrastructure.Persistence.Context;

namespace InkFlow.Infrastructure.Persistence.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    #region Mutations

    public virtual async Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public virtual async Task<bool> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public virtual Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbSet.Update(entity);
            return Task.FromResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public virtual Task<bool> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbSet.UpdateRange(entities);
            return Task.FromResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public virtual Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbSet.Remove(entity);
            return Task.FromResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public virtual async Task<bool> DeleteByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        try
        {
            var entity = await _dbSet.FindAsync([id], cancellationToken);
            if (entity == null)
            {
                return false;
            }

            _dbSet.Remove(entity);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public virtual async Task<bool> DeleteRangeByIdAsync<TId>(IEnumerable<TId> ids, CancellationToken cancellationToken = default) where TId : notnull
    {
        try
        {
            var entities = new List<TEntity>();
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entity = await _dbSet.FindAsync([id], cancellationToken);
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            if (entities.Count == 0)
            {
                return false;
            }

            _dbSet.RemoveRange(entities);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public virtual Task<bool> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dbSet.RemoveRange(entities);
            return Task.FromResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Queries

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        bool splitQuery = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, filterList, includes, orderBy, disableTracking, ignoreQueryFilter, splitQuery);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        bool splitQuery = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, filterList, includes, null, disableTracking, ignoreQueryFilter, splitQuery);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<PaginationResponse<TEntity>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        bool splitQuery = false,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var countQuery = BuildQuery(filter, filterList, null, null, disableTracking: true, ignoreQueryFilter, splitQuery: false);
        var totalCount = await countQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PaginationResponse<TEntity>([], 0, pageNumber, pageSize);
        }

        var query = BuildQuery(filter, filterList, includes, orderBy, disableTracking, ignoreQueryFilter, splitQuery);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResponse<TEntity>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<IReadOnlyList<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selectExpression,
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool distinct = false,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, filterList, null, orderBy, disableTracking, ignoreQueryFilter, splitQuery: false);
        var projectedQuery = query.Select(selectExpression);

        if (distinct)
        {
            projectedQuery = projectedQuery.Distinct();
        }

        return await projectedQuery.ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, filterList, null, null, disableTracking: true, ignoreQueryFilter, splitQuery: false);
        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter, filterList, null, null, disableTracking: true, ignoreQueryFilter, splitQuery: false);
        return await query.AnyAsync(cancellationToken);
    }

    public virtual async Task<Dictionary<TKey, TResult>> GetDictionaryAsync<TKey, TResult>(
        Expression<Func<TEntity, TKey>> keySelector,
        Expression<Func<TEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>>? filter = null,
        List<Expression<Func<TEntity, bool>>>? filterList = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        bool disableTracking = false,
        bool ignoreQueryFilter = false,
        CancellationToken cancellationToken = default) where TKey : notnull
    {
        var query = BuildQuery(filter, filterList, includes, orderBy, disableTracking, ignoreQueryFilter, splitQuery: false);
        return await query.ToDictionaryAsync<TEntity, TKey, TResult>(
            keySelector.Compile(),
            resultSelector.Compile(),
            cancellationToken);
    }

    #endregion

    #region Helper

    protected virtual IQueryable<TEntity> BuildQuery(
        Expression<Func<TEntity, bool>>? filter,
        List<Expression<Func<TEntity, bool>>>? filterList,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
        bool disableTracking,
        bool ignoreQueryFilter,
        bool splitQuery)
    {
        IQueryable<TEntity> query = _dbSet;

        if (disableTracking)
        {
            query = query.AsNoTracking();
        }

        if (ignoreQueryFilter)
        {
            query = query.IgnoreQueryFilters();
        }

        if (splitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (filterList != null && filterList.Count > 0)
        {
            foreach (var item in filterList)
            {
                query = query.Where(item);
            }
        }

        if (includes != null)
        {
            query = includes(query);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return query;
    }

    #endregion
}
