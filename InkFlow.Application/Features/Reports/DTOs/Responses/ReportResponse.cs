using InkFlow.Domain;

namespace InkFlow.Application.Features.Reports.DTOs.Responses;

public class ReportResponse
{
    public Guid ReportId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public ReportStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid ReporterUserId { get; init; }
    public string ReporterDisplayName { get; init; } = string.Empty;
    public Guid? ArticleId { get; init; }
    public Guid? CommentId { get; init; }
}
