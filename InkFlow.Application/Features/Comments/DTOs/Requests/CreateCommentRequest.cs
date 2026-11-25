namespace InkFlow.Application.Features.Comments.DTOs.Requests;

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}