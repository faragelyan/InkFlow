using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Reports.DTOs.Requests;
using InkFlow.Application.Features.Reports.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Reports.Interfaces;

public interface IReportService
{
    Task<Result<ReportResponse>> CreateAsync(Guid reporterId, CreateReportRequest request, CancellationToken cancellationToken = default);
    Task<Result<PaginationResponse<ReportResponse>>> GetMineAsync(Guid reporterId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<PaginationResponse<ReportResponse>>> GetModerationQueueAsync(ReportStatus? status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<ReportResponse>> UpdateStatusAsync(Guid reportId, UpdateReportStatusRequest request, CancellationToken cancellationToken = default);
}
