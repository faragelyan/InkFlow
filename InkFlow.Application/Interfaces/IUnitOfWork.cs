using InkFlow.Application.Interfaces.Repositories;

namespace InkFlow.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

