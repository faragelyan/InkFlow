using InkFlow.Application.Features.Reports.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Reports.Mappings;

public static class ReportMappingConfig
{
    public static ReportResponse ToResponse(Report report, AppUser? reporter)
    {
        return new ReportResponse
        {
            ReportId = report.ReportId,
            Reason = report.Reason,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
            ReporterUserId = report.ReporterUserId,
            ReporterDisplayName = reporter is { IsActive: true, IsDeleted: false }
                ? reporter.DisplayName
                : "Inactive user",
            ArticleId = report.ArticleId,
            CommentId = report.CommentId
        };
    }
}
