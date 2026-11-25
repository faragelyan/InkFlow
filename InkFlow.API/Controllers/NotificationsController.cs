using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Notifications;
using InkFlow.Application.Features.Notifications.DTOs.Responses;
using InkFlow.Application.Features.Notifications.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PaginationResponse<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _notificationService.GetMineAsync(userId, pageNumber, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPatch("me/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _notificationService.MarkAllReadAsync(userId, cancellationToken);
        if (result.IsFailure)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: result.Error);

        return NoContent();
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _notificationService.MarkReadAsync(userId, notificationId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == NotificationErrors.NotFound)
                return NotFound(new ProblemDetails { Title = "Notification not found", Detail = result.Error });

            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: result.Error);
        }

        return NoContent();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}