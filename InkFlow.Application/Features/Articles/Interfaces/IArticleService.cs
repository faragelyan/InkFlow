using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Articles.DTOs.Requests;
using InkFlow.Application.Features.Articles.DTOs.Responses;

namespace InkFlow.Application.Features.Articles.Interfaces;

public interface IArticleService
{
    Task<Result<ArticleResponse>> CreateAsync(
        Guid authorId,
        CreateArticleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetPublishedAsync(
        Guid? authorId,
        int pageNumber = 1,
        int pageSize = 10,
        string? topicSlug = null,
        string? tagSlug = null,
        Guid? viewerId = null,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleResponse>> GetPublishedByIdAsync(Guid articleId, Guid? viewerId = null, CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetMineAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetMyBookmarksAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleResponse>> GetMineByIdAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleResponse>> UpdateAsync(
        Guid authorId,
        Guid articleId,
        UpdateArticleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleEngagementResponse>> LikeAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default);
    Task<Result<ArticleEngagementResponse>> UnlikeAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default);
    Task<Result<ArticleEngagementResponse>> BookmarkAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default);
    Task<Result<ArticleEngagementResponse>> RemoveBookmarkAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default);

    Task<Result<ArticleResponse>> PublishAsync(Guid authorId, Guid articleId, CancellationToken cancellationToken = default);
    Task<Result<ArticleResponse>> UnpublishAsync(Guid authorId, Guid articleId, CancellationToken cancellationToken = default);
    Task<Result<ArticleResponse>> ArchiveAsync(Guid authorId, Guid articleId, CancellationToken cancellationToken = default);
    Task<Result<ArticleResponse>> UnarchiveAsync(Guid authorId, Guid articleId, CancellationToken cancellationToken = default);
}