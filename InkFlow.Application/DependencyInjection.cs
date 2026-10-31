using Microsoft.Extensions.DependencyInjection;

namespace InkFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services, handlers, validators, and behaviors here
        return services;
    }
}
