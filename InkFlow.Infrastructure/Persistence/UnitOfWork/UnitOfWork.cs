using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Infrastructure.Persistence.Context;
using InkFlow.Infrastructure.Persistence.Repositories;

namespace InkFlow.Infrastructure.Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        return new GenericRepository<TEntity>(_context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

