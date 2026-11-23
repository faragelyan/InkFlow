namespace InkFlow.Application.Features.Authors.DTOs.Requests;

public class UpdateAuthorProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
}