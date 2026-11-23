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
        var result = await _authorService.GetProfileAsync(authorId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = result.Error });

        return Ok(result.Value);
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

        var result = await _authorService.GetProfileAsync(authorId, cancellationToken);
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
}