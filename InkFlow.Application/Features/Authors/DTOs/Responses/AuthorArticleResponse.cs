namespace InkFlow.Application.Features.Authors.DTOs.Responses;

public class AuthorArticleResponse
{
    public Guid ArticleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public DateTime? PublishedAt { get; init; }
}