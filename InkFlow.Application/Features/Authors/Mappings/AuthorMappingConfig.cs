using InkFlow.Application.Features.Authors.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Authors.Mappings;

public static class AuthorMappingConfig
{
    public static AuthorProfileResponse ToProfileResponse(
        AppUser author,
        int publishedArticleCount,
        int followerCount)
    {
        return new AuthorProfileResponse
        {
            AuthorId = author.Id,
            DisplayName = author.DisplayName,
            Bio = author.Bio,
            ProfileImageUrl = author.ProfileImageUrl,
            JoinedAt = author.CreatedAt,
            PublishedArticleCount = publishedArticleCount,
            FollowerCount = followerCount
        };
    }

    public static AuthorArticleResponse ToArticleResponse(Article article)
    {
        return new AuthorArticleResponse
        {
            ArticleId = article.ArticleId,
            Title = article.Title,
            Subtitle = article.Subtitle,
            Slug = article.Slug,
            CoverImageUrl = article.CoverImageUrl,
            PublishedAt = article.PublishedAt
        };
    }
}