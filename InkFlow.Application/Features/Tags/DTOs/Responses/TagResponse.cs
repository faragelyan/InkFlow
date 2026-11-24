namespace InkFlow.Application.Features.Tags.DTOs.Responses;

public class TagResponse
{
    public Guid TagId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int ArticleCount { get; init; }
}
