using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Tags.DTOs.Requests;
using InkFlow.Application.Features.Tags.DTOs.Responses;

namespace InkFlow.Application.Features.Tags.Interfaces;

public interface ITagService
{
    Task<Result<PaginationResponse<TagResponse>>> GetAsync(string? search, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> UpdateAsync(Guid tagId, UpdateTagRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid tagId, CancellationToken cancellationToken = default);
}
