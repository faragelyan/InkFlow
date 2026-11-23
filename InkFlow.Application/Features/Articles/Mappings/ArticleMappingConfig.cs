using InkFlow.Application.Features.Articles.DTOs.Responses;
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
            ArchivedAt = article.ArchivedAt
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
            PublishedAt = article.PublishedAt
        };
    }
}