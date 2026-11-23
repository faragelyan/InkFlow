using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using InkFlow.Application.Features.Authors.Interfaces;
using InkFlow.Application.Features.Authors.Services;

namespace InkFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IAuthorService, AuthorService>();
        return services;
    }
}
