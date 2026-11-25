namespace InkFlow.Application.Features.Reports.DTOs.Requests;

public class CreateReportRequest
{
    public string Reason { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public Guid? CommentId { get; set; }
}
