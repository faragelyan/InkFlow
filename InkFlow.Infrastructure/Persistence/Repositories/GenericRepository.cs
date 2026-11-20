using InkFlow.Application.Interfaces.Repositories;

namespace InkFlow.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
}

