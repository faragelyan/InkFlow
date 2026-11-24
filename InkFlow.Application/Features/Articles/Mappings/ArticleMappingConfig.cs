using InkFlow.Application.Features.Articles.DTOs.Responses;
using InkFlow.Application.Features.Tags.DTOs.Responses;
using InkFlow.Application.Features.Tags.Mappings;
using InkFlow.Application.Features.Topics.DTOs.Responses;
using InkFlow.Application.Features.Topics.Mappings;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Articles.Mappings;

public static class ArticleMappingConfig
{
    public static ArticleResponse ToResponse(Article article)
    {
        return new ArticleResponse
        {
            ArticleId = article.ArticleId,
            AuthorId = article.UserId,
            Title = article.Title,
            Subtitle = article.Subtitle,
            Content = article.Content,
            Slug = article.Slug,
            CoverImageUrl = article.CoverImageUrl,
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            PublishedAt = article.PublishedAt,
            ArchivedAt = article.ArchivedAt,
            Topics = article.ArticleTopics.Select(link => link.Topic).Where(topic => topic != null).Select(TopicMappingConfig.ToSummaryResponse).ToList(),
            Tags = article.ArticleTags.Select(link => link.Tag).Where(tag => tag != null).Select(TagMappingConfig.ToSummaryResponse).ToList()
        };
    }

    public static ArticleSummaryResponse ToSummaryResponse(Article article)
    {
        return new ArticleSummaryResponse
        {
            ArticleId = article.ArticleId,
            AuthorId = article.UserId,
            Title = article.Title,
            Subtitle = article.Subtitle,
            Slug = article.Slug,
            CoverImageUrl = article.CoverImageUrl,
            Status = article.Status,
            PublishedAt = article.PublishedAt,
            Topics = article.ArticleTopics.Select(link => link.Topic).Where(topic => topic != null).Select(TopicMappingConfig.ToSummaryResponse).ToList(),
            Tags = article.ArticleTags.Select(link => link.Tag).Where(tag => tag != null).Select(TagMappingConfig.ToSummaryResponse).ToList()
        };
    }
}