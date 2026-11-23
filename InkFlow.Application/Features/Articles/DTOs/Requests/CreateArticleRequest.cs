namespace InkFlow.Application.Features.Articles.DTOs.Requests;

public class CreateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
}