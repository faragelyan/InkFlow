namespace InkFlow.Application.Features.Topics.DTOs.Responses;

public class TopicSummaryResponse
{
    public Guid TopicId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}
