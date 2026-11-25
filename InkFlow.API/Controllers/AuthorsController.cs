using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Authors;
using InkFlow.Application.Features.Authors.DTOs.Requests;
using InkFlow.Application.Features.Authors.DTOs.Responses;
using InkFlow.Application.Features.Authors.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpGet("{authorId:guid}")]
    [ProducesResponseType(typeof(AuthorProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid authorId, CancellationToken cancellationToken)
    {
        var viewerId = TryGetCurrentUserId(out var currentUserId) ? currentUserId : (Guid?)null;
        var result = await _authorService.GetProfileAsync(authorId, cancellationToken, viewerId);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{authorId:guid}/followers")]
    [ProducesResponseType(typeof(PaginationResponse<AuthorConnectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFollowers(
        Guid authorId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _authorService.GetFollowersAsync(authorId, pageNumber, pageSize, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{authorId:guid}/following")]
    [ProducesResponseType(typeof(PaginationResponse<AuthorConnectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFollowing(
        Guid authorId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _authorService.GetFollowingAsync(authorId, pageNumber, pageSize, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPut("{authorId:guid}/follow")]
    [ProducesResponseType(typeof(AuthorProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Follow(Guid authorId, CancellationToken cancellationToken)
    {
        return ChangeFollowAsync(authorId, _authorService.FollowAsync, cancellationToken);
    }

    [Authorize]
    [HttpDelete("{authorId:guid}/follow")]
    [ProducesResponseType(typeof(AuthorProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Unfollow(Guid authorId, CancellationToken cancellationToken)
    {
        return ChangeFollowAsync(authorId, _authorService.UnfollowAsync, cancellationToken);
    }

    [HttpGet("{authorId:guid}/articles")]
    [ProducesResponseType(typeof(PaginationResponse<AuthorArticleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedArticles(
        Guid authorId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _authorService.GetPublishedArticlesAsync(
            authorId, pageNumber, pageSize, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthorProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await _authorService.GetProfileAsync(authorId, cancellationToken, authorId);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType(typeof(AuthorProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateAuthorProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await _authorService.UpdateProfileAsync(authorId, request, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == AuthorErrors.NotFound)
                return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

            if (result.Error == AuthorErrors.ProfileUpdateFailed)
                return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: result.Error);

            return BadRequest(new ProblemDetails
            {
                Title = "Profile update failed",
                Detail = result.Error,
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result.Value);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private async Task<IActionResult> ChangeFollowAsync(
        Guid authorId,
        Func<Guid, Guid, CancellationToken, Task<InkFlow.Application.Common.Results.Result<AuthorProfileResponse>>> operation,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var followerId))
            return Unauthorized();

        var result = await operation(followerId, authorId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == AuthorErrors.NotFound)
                return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

            return BadRequest(new ProblemDetails { Title = "Follow operation failed", Detail = result.Error });
        }

        return Ok(result.Value);
    }
}