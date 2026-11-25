using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Comments.DTOs.Requests;
using InkFlow.Application.Features.Comments.DTOs.Responses;

namespace InkFlow.Application.Features.Comments.Interfaces;

public interface ICommentService
{
    Task<Result<PaginationResponse<CommentResponse>>> GetArticleCommentsAsync(
        Guid articleId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<CommentResponse>>> GetRepliesAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Result<CommentResponse>> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);

    Task<Result<CommentResponse>> CreateAsync(
        Guid userId,
        Guid articleId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CommentResponse>> UpdateAsync(
        Guid userId,
        Guid commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default);
}