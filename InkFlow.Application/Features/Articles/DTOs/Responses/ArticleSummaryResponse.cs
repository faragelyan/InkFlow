using InkFlow.Application.Features.Tags.DTOs.Responses;
using InkFlow.Application.Features.Topics.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Articles.DTOs.Responses;

public class ArticleSummaryResponse
{
    public Guid ArticleId { get; init; }
    public Guid AuthorId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public ArticleStatus Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public IReadOnlyList<TopicSummaryResponse> Topics { get; init; } = [];
    public IReadOnlyList<TagSummaryResponse> Tags { get; init; } = [];
    public int LikeCount { get; init; }
    public int BookmarkCount { get; init; }
    public bool IsLiked { get; init; }
    public bool IsBookmarked { get; init; }
}