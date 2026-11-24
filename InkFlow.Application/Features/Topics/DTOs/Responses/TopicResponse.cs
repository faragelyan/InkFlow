using InkFlow.Domain;

namespace InkFlow.Application.Features.Topics.DTOs.Responses;

public class TopicResponse
{
    public Guid TopicId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int ArticleCount { get; init; }
}
