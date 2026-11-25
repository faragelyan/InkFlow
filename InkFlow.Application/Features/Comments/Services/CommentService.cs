using FluentValidation;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Comments.DTOs.Requests;
using InkFlow.Application.Features.Comments.DTOs.Responses;
using InkFlow.Application.Features.Comments.Interfaces;
using InkFlow.Application.Features.Comments.Mappings;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Comments.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Comment> _comments;
    private readonly IGenericRepository<Article> _articles;
    private readonly IGenericRepository<AppUser> _users;
    private readonly IValidator<CreateCommentRequest> _createValidator;
    private readonly IValidator<UpdateCommentRequest> _updateValidator;

    public CommentService(
        IUnitOfWork unitOfWork,
        IValidator<CreateCommentRequest> createValidator,
        IValidator<UpdateCommentRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _comments = unitOfWork.GetRepository<Comment>();
        _articles = unitOfWork.GetRepository<Article>();
        _users = unitOfWork.GetRepository<AppUser>();
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PaginationResponse<CommentResponse>>> GetArticleCommentsAsync(
        Guid articleId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!await IsPublishedArticleAsync(articleId, cancellationToken))
            return Result<PaginationResponse<CommentResponse>>.Failure(CommentErrors.ArticleNotFound);

        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var page = await _comments.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: comment => comment.ArticleId == articleId && comment.ParentCommentId == null,
            orderBy: query => query.OrderBy(comment => comment.CreatedAt),
            disableTracking: true,
            ignoreQueryFilter: true,
            cancellationToken: cancellationToken);

        return Result<PaginationResponse<CommentResponse>>.Success(await MapPageAsync(page, cancellationToken));
    }

    public async Task<Result<PaginationResponse<CommentResponse>>> GetRepliesAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var parent = await _comments.GetSingleAsync(
            comment => comment.CommentId == commentId,
            disableTracking: true,
            ignoreQueryFilter: true,
            cancellationToken: cancellationToken);
        if (parent is null || !await IsPublishedArticleAsync(parent.ArticleId, cancellationToken))
            return Result<PaginationResponse<CommentResponse>>.Failure(CommentErrors.CommentNotFound);

        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var page = await _comments.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: comment => comment.ParentCommentId == commentId,
            orderBy: query => query.OrderBy(comment => comment.CreatedAt),
            disableTracking: true,
            ignoreQueryFilter: true,
            cancellationToken: cancellationToken);

        return Result<PaginationResponse<CommentResponse>>.Success(await MapPageAsync(page, cancellationToken));
    }

    public async Task<Result<CommentResponse>> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _comments.GetSingleAsync(
            item => item.CommentId == commentId,
            disableTracking: true,
            ignoreQueryFilter: true,
            cancellationToken: cancellationToken);
        if (comment is null || !await IsPublishedArticleAsync(comment.ArticleId, cancellationToken))
            return Result<CommentResponse>.Failure(CommentErrors.CommentNotFound);

        var response = await MapCommentsAsync([comment], cancellationToken);
        return Result<CommentResponse>.Success(response[0]);
    }

    public async Task<Result<CommentResponse>> CreateAsync(
        Guid userId,
        Guid articleId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<CommentResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        if (!await IsPublishedArticleAsync(articleId, cancellationToken))
            return Result<CommentResponse>.Failure(CommentErrors.ArticleNotFound);

        var author = await _users.GetSingleAsync(
            user => user.Id == userId && user.IsActive && !user.IsDeleted,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (author is null)
            return Result<CommentResponse>.Failure(CommentErrors.AuthorNotFound);

        if (request.ParentCommentId is Guid parentCommentId)
        {
            var parent = await _comments.GetSingleAsync(
                comment => comment.CommentId == parentCommentId,
                disableTracking: true,
                ignoreQueryFilter: true,
                cancellationToken: cancellationToken);
            if (parent is null || parent.ArticleId != articleId)
                return Result<CommentResponse>.Failure(CommentErrors.ParentCommentNotFound);
        }

        var comment = new Comment
        {
            CommentId = Guid.NewGuid(),
            UserId = userId,
            ArticleId = articleId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim()
        };

        if (!await _comments.AddAsync(comment, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<CommentResponse>.Failure(CommentErrors.SaveFailed);

        var response = await MapCommentsAsync([comment], cancellationToken);
        return Result<CommentResponse>.Success(response[0]);
    }

    public async Task<Result<CommentResponse>> UpdateAsync(
        Guid userId,
        Guid commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<CommentResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var comment = await _comments.GetSingleAsync(
            item => item.CommentId == commentId && item.UserId == userId,
            cancellationToken: cancellationToken);
        if (comment is null)
            return Result<CommentResponse>.Failure(CommentErrors.CommentNotFound);

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        if (!await _comments.UpdateAsync(comment, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<CommentResponse>.Failure(CommentErrors.UpdateFailed);

        var response = await MapCommentsAsync([comment], cancellationToken);
        return Result<CommentResponse>.Success(response[0]);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _comments.GetSingleAsync(
            item => item.CommentId == commentId && item.UserId == userId,
            cancellationToken: cancellationToken);
        if (comment is null)
            return Result.Failure(CommentErrors.CommentNotFound);

        if (!await _comments.DeleteAsync(comment, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result.Failure(CommentErrors.DeleteFailed);

        return Result.Success();
    }

    private async Task<bool> IsPublishedArticleAsync(Guid articleId, CancellationToken cancellationToken)
    {
        return await _articles.ExistsAsync(
            article => article.ArticleId == articleId && article.Status == ArticleStatus.Published,
            cancellationToken: cancellationToken);
    }

    private async Task<PaginationResponse<CommentResponse>> MapPageAsync(
        PaginationResponse<Comment> page,
        CancellationToken cancellationToken)
    {
        var items = await MapCommentsAsync(page.Items, cancellationToken);
        return new PaginationResponse<CommentResponse>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    private async Task<IReadOnlyList<CommentResponse>> MapCommentsAsync(
        IReadOnlyCollection<Comment> comments,
        CancellationToken cancellationToken)
    {
        if (comments.Count == 0)
            return [];

        var userIds = comments.Select(comment => comment.UserId).Distinct().ToList();
        var users = await _users.GetAllAsync(
            user => userIds.Contains(user.Id),
            disableTracking: true,
            cancellationToken: cancellationToken);
        var userLookup = users.ToDictionary(user => user.Id);
        var responses = new List<CommentResponse>(comments.Count);

        foreach (var comment in comments)
        {
            var replyCount = await _comments.CountAsync(
                reply => reply.ParentCommentId == comment.CommentId,
                ignoreQueryFilter: true,
                cancellationToken: cancellationToken);
            var hasActiveAuthor = userLookup.TryGetValue(comment.UserId, out var author)
                && author.IsActive
                && !author.IsDeleted;
            var displayName = hasActiveAuthor ? author!.DisplayName : "Deleted user";
            responses.Add(CommentMappingConfig.ToResponse(comment, displayName, replyCount));
        }

        return responses;
    }
}