using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InkFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register database contexts, external clients, repositories, and third-party adapters here
        return services;
    }
}
