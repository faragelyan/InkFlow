using InkFlow.Domain;

namespace InkFlow.Application.Features.Reports.DTOs.Requests;

public class UpdateReportStatusRequest
{
    public ReportStatus Status { get; set; }
}
