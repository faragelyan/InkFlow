using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using InkFlow.Application.Features.Articles.Interfaces;
using InkFlow.Application.Features.Articles.Services;
using InkFlow.Application.Features.Authors.Interfaces;
using InkFlow.Application.Features.Authors.Services;
using InkFlow.Application.Features.Topics.Interfaces;
using InkFlow.Application.Features.Topics.Services;
using InkFlow.Application.Features.Tags.Interfaces;
using InkFlow.Application.Features.Tags.Services;

namespace InkFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<ITagService, TagService>();
        return services;
    }
}
