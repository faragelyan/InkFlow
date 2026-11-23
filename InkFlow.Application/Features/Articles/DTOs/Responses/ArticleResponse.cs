using InkFlow.Domain;

namespace InkFlow.Application.Features.Articles.DTOs.Responses;

public class ArticleResponse
{
    public Guid ArticleId { get; init; }
    public Guid AuthorId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public ArticleStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }
}