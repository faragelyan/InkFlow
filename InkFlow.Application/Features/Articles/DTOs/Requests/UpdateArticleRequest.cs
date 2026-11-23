namespace InkFlow.Application.Features.Articles.DTOs.Requests;

public class UpdateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
}