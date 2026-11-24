namespace InkFlow.Application.Features.Tags.DTOs.Responses;

public class TagSummaryResponse
{
    public Guid TagId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}
