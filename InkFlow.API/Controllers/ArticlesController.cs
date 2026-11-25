using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Articles;
using InkFlow.Application.Features.Articles.DTOs.Requests;
using InkFlow.Application.Features.Articles.DTOs.Responses;
using InkFlow.Application.Features.Articles.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticlesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginationResponse<ArticleSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublished(
        [FromQuery] Guid? authorId,
        [FromQuery] string? topicSlug,
        [FromQuery] string? tagSlug,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _articleService.GetPublishedAsync(authorId, pageNumber, pageSize, topicSlug, tagSlug, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{articleId:guid}")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedById(Guid articleId, CancellationToken cancellationToken)
    {
        var result = await _articleService.GetPublishedByIdAsync(articleId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Article not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await _articleService.CreateAsync(authorId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return CreatedAtAction(
            nameof(GetMineById),
            new { articleId = result.Value.ArticleId },
            result.Value);
    }

    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PaginationResponse<ArticleSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await _articleService.GetMineAsync(authorId, pageNumber, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("mine/{articleId:guid}")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMineById(Guid articleId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await _articleService.GetMineByIdAsync(authorId, articleId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Article not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPut("{articleId:guid}")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        Guid articleId,
        [FromBody] UpdateArticleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await _articleService.UpdateAsync(authorId, articleId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPatch("{articleId:guid}/publish")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Publish(Guid articleId, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(articleId, _articleService.PublishAsync, cancellationToken);
    }

    [Authorize]
    [HttpPatch("{articleId:guid}/unpublish")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Unpublish(Guid articleId, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(articleId, _articleService.UnpublishAsync, cancellationToken);
    }

    [Authorize]
    [HttpPatch("{articleId:guid}/archive")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Archive(Guid articleId, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(articleId, _articleService.ArchiveAsync, cancellationToken);
    }

    [Authorize]
    [HttpPatch("{articleId:guid}/unarchive")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Unarchive(Guid articleId, CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(articleId, _articleService.UnarchiveAsync, cancellationToken);
    }

    private async Task<IActionResult> ChangeStatusAsync(
        Guid articleId,
        Func<Guid, Guid, CancellationToken, Task<InkFlow.Application.Common.Results.Result<ArticleResponse>>> changeStatus,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var authorId))
            return Unauthorized();

        var result = await changeStatus(authorId, articleId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == ArticleErrors.ArticleNotFound)
                return NotFound(new ProblemDetails { Title = "Article not found", Detail = result.Error });

            return Conflict(new ProblemDetails { Title = "Article status conflict", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    private IActionResult MapWriteFailure(string? error, IReadOnlyList<string> errors)
    {
        if (error == ArticleErrors.AuthorNotFound)
            return NotFound(new ProblemDetails { Title = "Author not found", Detail = error });

        if (error == ArticleErrors.ArticleNotFound)
            return NotFound(new ProblemDetails { Title = "Article not found", Detail = error });

        if (error == ArticleErrors.ArticleUnavailable)
            return Conflict(new ProblemDetails { Title = "Article status conflict", Detail = error });

        if (error is ArticleErrors.SaveFailed or ArticleErrors.UpdateFailed)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: error);

        return BadRequest(new ProblemDetails
        {
            Title = "Article operation failed",
            Detail = error,
            Extensions = { ["errors"] = errors }
        });
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}