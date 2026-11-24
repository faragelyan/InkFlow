using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Features.Tags;
using InkFlow.Application.Features.Tags.DTOs.Requests;
using InkFlow.Application.Features.Tags.DTOs.Responses;
using InkFlow.Application.Features.Tags.Interfaces;

namespace InkFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginationResponse<TagResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _tagService.GetAsync(search, pageNumber, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _tagService.GetBySlugAsync(slug, cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Tag not found", Detail = result.Error });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await _tagService.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Value.Slug }, result.Value);
    }

    [Authorize]
    [HttpPut("{tagId:guid}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid tagId, [FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await _tagService.UpdateAsync(tagId, request, cancellationToken);
        if (result.IsFailure)
            return MapWriteFailure(result.Error, result.Errors);

        return Ok(result.Value);
    }

    [Authorize]
    [HttpDelete("{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid tagId, CancellationToken cancellationToken)
    {
        var result = await _tagService.DeleteAsync(tagId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == TagErrors.TagNotFound)
                return NotFound(new ProblemDetails { Title = "Tag not found", Detail = result.Error });

            return Conflict(new ProblemDetails { Title = "Tag delete conflict", Detail = result.Error });
        }

        return NoContent();
    }

    private IActionResult MapWriteFailure(string? error, IReadOnlyList<string> errors)
    {
        if (error == TagErrors.TagNotFound)
            return NotFound(new ProblemDetails { Title = "Tag not found", Detail = error });

        if (error is TagErrors.DuplicateName or TagErrors.DuplicateSlug or TagErrors.TagInUse)
            return Conflict(new ProblemDetails { Title = "Tag operation conflict", Detail = error, Extensions = { ["errors"] = errors } });

        return BadRequest(new ProblemDetails { Title = "Tag operation failed", Detail = error, Extensions = { ["errors"] = errors } });
    }
}
