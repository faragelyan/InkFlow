using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Topics;
using InkFlow.Application.Features.Topics.DTOs.Requests;
using InkFlow.Application.Features.Topics.DTOs.Responses;
using InkFlow.Application.Features.Topics.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;

    public TopicsController(ITopicService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginationResponse<TopicResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _topicService.GetAsync(search, pageNumber, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(TopicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _topicService.GetBySlugAsync(slug, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Topic not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(TopicResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await _topicService.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Value.Slug }, result.Value);
    }

    [Authorize]
    [HttpPut("{topicId:guid}")]
    [ProducesResponseType(typeof(TopicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid topicId, [FromBody] UpdateTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await _topicService.UpdateAsync(topicId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return Ok(result.Value);
    }

    [Authorize]
    [HttpDelete("{topicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid topicId, CancellationToken cancellationToken)
    {
        var result = await _topicService.DeleteAsync(topicId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == TopicErrors.TopicNotFound)
                return NotFound(new ProblemDetails { Title = "Topic not found", Detail = result.Error });

            return Conflict(new ProblemDetails { Title = "Topic delete conflict", Detail = result.Error });
        }

        return NoContent();
    }

    private IActionResult MapWriteFailure(string? error, IReadOnlyList<string> errors)
    {
        if (error == TopicErrors.TopicNotFound)
            return NotFound(new ProblemDetails { Title = "Topic not found", Detail = error });

        if (error is TopicErrors.DuplicateName or TopicErrors.DuplicateSlug or TopicErrors.TopicInUse)
            return Conflict(new ProblemDetails { Title = "Topic operation conflict", Detail = error, Extensions = { ["errors"] = errors } });

        return BadRequest(new ProblemDetails { Title = "Topic operation failed", Detail = error, Extensions = { ["errors"] = errors } });
    }
}
