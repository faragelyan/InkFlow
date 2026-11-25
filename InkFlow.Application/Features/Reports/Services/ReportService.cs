using FluentValidation;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Reports.DTOs.Requests;
using InkFlow.Application.Features.Reports.DTOs.Responses;
using InkFlow.Application.Features.Reports.Interfaces;
using InkFlow.Application.Features.Reports.Mappings;
using InkFlow.Application.Features.Reports.Validators;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Reports.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Report> _reports;
    private readonly IGenericRepository<Article> _articles;
    private readonly IGenericRepository<Comment> _comments;
    private readonly IGenericRepository<AppUser> _users;
    private readonly IValidator<CreateReportRequest> _createValidator;
    private readonly IValidator<UpdateReportStatusRequest> _updateStatusValidator;

    public ReportService(
        IUnitOfWork unitOfWork,
        IValidator<CreateReportRequest> createValidator,
        IValidator<UpdateReportStatusRequest> updateStatusValidator)
    {
        _unitOfWork = unitOfWork;
        _reports = unitOfWork.GetRepository<Report>();
        _articles = unitOfWork.GetRepository<Article>();
        _comments = unitOfWork.GetRepository<Comment>();
        _users = unitOfWork.GetRepository<AppUser>();
        _createValidator = createValidator;
        _updateStatusValidator = updateStatusValidator;
    }

    public async Task<Result<ReportResponse>> CreateAsync(
        Guid reporterId,
        CreateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<ReportResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var reporter = await _users.GetSingleAsync(
            user => user.Id == reporterId && user.IsActive && !user.IsDeleted,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (reporter is null)
            return Result<ReportResponse>.Failure(ReportErrors.ActiveUserNotFound);

        if (request.ArticleId is Guid articleId)
        {
            if (!await _articles.ExistsAsync(
                    article => article.ArticleId == articleId && article.Status == ArticleStatus.Published,
                    cancellationToken: cancellationToken))
                return Result<ReportResponse>.Failure(ReportErrors.ArticleNotFound);

            if (await _reports.ExistsAsync(
                    report => report.ReporterUserId == reporterId
                        && report.ArticleId == articleId
                        && (report.Status == ReportStatus.Pending || report.Status == ReportStatus.UnderReview),
                    cancellationToken: cancellationToken))
                return Result<ReportResponse>.Failure(ReportErrors.Duplicate);
        }
        else
        {
            var commentId = request.CommentId!.Value;
            if (!await _comments.ExistsAsync(
                    comment => comment.CommentId == commentId
                        && !comment.IsDeleted
                        && comment.Article.Status == ArticleStatus.Published,
                    ignoreQueryFilter: true,
                    cancellationToken: cancellationToken))
                return Result<ReportResponse>.Failure(ReportErrors.CommentNotFound);

            if (await _reports.ExistsAsync(
                    report => report.ReporterUserId == reporterId
                        && report.CommentId == commentId
                        && (report.Status == ReportStatus.Pending || report.Status == ReportStatus.UnderReview),
                    cancellationToken: cancellationToken))
                return Result<ReportResponse>.Failure(ReportErrors.Duplicate);
        }

        var report = new Report
        {
            ReportId = Guid.NewGuid(),
            Reason = request.Reason.Trim(),
            Status = ReportStatus.Pending,
            ReporterUserId = reporterId,
            ArticleId = request.ArticleId,
            CommentId = request.CommentId
        };

        if (!await _reports.AddAsync(report, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<ReportResponse>.Failure(ReportErrors.SaveFailed);

        return Result<ReportResponse>.Success(ReportMappingConfig.ToResponse(report, reporter));
    }

    public async Task<Result<PaginationResponse<ReportResponse>>> GetMineAsync(
        Guid reporterId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var page = await _reports.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: report => report.ReporterUserId == reporterId,
            orderBy: query => query.OrderByDescending(report => report.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var reporter = await _users.GetSingleAsync(
            user => user.Id == reporterId,
            disableTracking: true,
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(report => ReportMappingConfig.ToResponse(report, reporter))
            .ToList();

        return Result<PaginationResponse<ReportResponse>>.Success(
            new PaginationResponse<ReportResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Result<PaginationResponse<ReportResponse>>> GetModerationQueueAsync(
        ReportStatus? status,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var page = await _reports.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: report => status == null
                || report.Status == status.Value,
            orderBy: query => query
                .OrderBy(report => report.Status == ReportStatus.Pending ? 0 : 1)
                .ThenBy(report => report.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var reporterIds = page.Items.Select(report => report.ReporterUserId).Distinct().ToList();
        var reporters = reporterIds.Count == 0
            ? []
            : await _users.GetAllAsync(
                user => reporterIds.Contains(user.Id),
                disableTracking: true,
                cancellationToken: cancellationToken);
        var reporterLookup = reporters.ToDictionary(user => user.Id);

        var items = page.Items
            .Select(report => ReportMappingConfig.ToResponse(
                report,
                reporterLookup.GetValueOrDefault(report.ReporterUserId)))
            .ToList();

        return Result<PaginationResponse<ReportResponse>>.Success(
            new PaginationResponse<ReportResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Result<ReportResponse>> UpdateStatusAsync(
        Guid reportId,
        UpdateReportStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validation = await _updateStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<ReportResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var report = await _reports.GetSingleAsync(
            item => item.ReportId == reportId,
            cancellationToken: cancellationToken);
        if (report is null)
            return Result<ReportResponse>.Failure(ReportErrors.NotFound);

        if (report.Status is ReportStatus.Resolved or ReportStatus.Dismissed)
            return Result<ReportResponse>.Failure(ReportErrors.UpdateFailed);

        report.Status = request.Status;
        if (!await _reports.UpdateAsync(report, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<ReportResponse>.Failure(ReportErrors.UpdateFailed);

        var reporter = await _users.GetSingleAsync(
            user => user.Id == report.ReporterUserId,
            disableTracking: true,
            cancellationToken: cancellationToken);

        return Result<ReportResponse>.Success(ReportMappingConfig.ToResponse(report, reporter));
    }
}
