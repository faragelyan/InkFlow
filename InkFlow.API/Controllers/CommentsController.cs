using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Comments;
using InkFlow.Application.Features.Comments.DTOs.Requests;
using InkFlow.Application.Features.Comments.DTOs.Responses;
using InkFlow.Application.Features.Comments.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Route("api")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("articles/{articleId:guid}/comments")]
    [ProducesResponseType(typeof(PaginationResponse<CommentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleComments(
        Guid articleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _commentService.GetArticleCommentsAsync(articleId, pageNumber, pageSize, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Article not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("comments/{commentId:guid}")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid commentId, CancellationToken cancellationToken)
    {
        var result = await _commentService.GetByIdAsync(commentId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Comment not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("comments/{commentId:guid}/replies")]
    [ProducesResponseType(typeof(PaginationResponse<CommentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReplies(
        Guid commentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _commentService.GetRepliesAsync(commentId, pageNumber, pageSize, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Comment not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("articles/{articleId:guid}/comments")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        Guid articleId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _commentService.CreateAsync(userId, articleId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return CreatedAtAction(nameof(GetById), new { commentId = result.Value.CommentId }, result.Value);
    }

    [Authorize]
    [HttpPut("comments/{commentId:guid}")]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        Guid commentId,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _commentService.UpdateAsync(userId, commentId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return Ok(result.Value);
    }

    [Authorize]
    [HttpDelete("comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid commentId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _commentService.DeleteAsync(userId, commentId, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return NoContent();
    }

    private IActionResult MapWriteFailure(string? error, IReadOnlyList<string> errors)
    {
        if (error is CommentErrors.ArticleNotFound or CommentErrors.CommentNotFound or CommentErrors.ParentCommentNotFound or CommentErrors.AuthorNotFound)
            return NotFound(new ProblemDetails { Title = "Comment resource not found", Detail = error });

        if (error is CommentErrors.SaveFailed or CommentErrors.UpdateFailed or CommentErrors.DeleteFailed)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: error);

        return BadRequest(new ProblemDetails
        {
            Title = "Comment operation failed",
            Detail = error,
            Extensions = { ["errors"] = errors }
        });
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}