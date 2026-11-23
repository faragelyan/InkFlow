using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Authors.DTOs.Requests;
using InkFlow.Application.Features.Authors.DTOs.Responses;

namespace InkFlow.Application.Features.Authors.Interfaces;

public interface IAuthorService
{
    Task<Result<AuthorProfileResponse>> GetProfileAsync(Guid authorId, CancellationToken cancellationToken = default);

    Task<Result<AuthorProfileResponse>> UpdateProfileAsync(
        Guid authorId,
        UpdateAuthorProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<AuthorArticleResponse>>> GetPublishedArticlesAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}