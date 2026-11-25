using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Reports;
using InkFlow.Application.Features.Reports.DTOs.Requests;
using InkFlow.Application.Features.Reports.DTOs.Responses;
using InkFlow.Application.Features.Reports.Interfaces;
using InkFlow.Domain;

namespace InkFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReportResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var reporterId))
            return Unauthorized();

        var result = await _reportService.CreateAsync(reporterId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return CreatedAtAction(nameof(GetMine), null, result.Value);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PaginationResponse<ReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var reporterId))
            return Unauthorized();

        var result = await _reportService.GetMineAsync(reporterId, pageNumber, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResponse<ReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] ReportStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetModerationQueueAsync(status, pageNumber, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{reportId:guid}/status")]
    [ProducesResponseType(typeof(ReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(
        Guid reportId,
        [FromBody] UpdateReportStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.UpdateStatusAsync(reportId, request, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == ReportErrors.NotFound)
                return NotFound(new ProblemDetails { Title = "Report not found", Detail = result.Error });

            if (result.Error == ReportErrors.UpdateFailed)
                return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: result.Error);

            return BadRequest(new ProblemDetails
            {
                Title = "Report status update failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result.Value);
    }

    private IActionResult MapWriteFailure(string? error, IReadOnlyList<string> errors)
    {
        if (error is ReportErrors.ArticleNotFound or ReportErrors.CommentNotFound or ReportErrors.ActiveUserNotFound)
            return NotFound(new ProblemDetails { Title = "Report resource not found", Detail = error });

        if (error == ReportErrors.Duplicate)
            return Conflict(new ProblemDetails { Title = "Duplicate report", Detail = error, Extensions = { ["errors"] = errors } });

        if (error == ReportErrors.SaveFailed)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: error);

        return BadRequest(new ProblemDetails
        {
            Title = "Report creation failed",
            Detail = error,
            Extensions = { ["errors"] = errors }
        });
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
