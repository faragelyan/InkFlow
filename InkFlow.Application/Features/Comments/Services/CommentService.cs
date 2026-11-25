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
    private readonly IGenericRepository<Notification> _notifications;
    private readonly ICommentEngagementRepository _commentEngagement;
    private readonly IValidator<CreateCommentRequest> _createValidator;
    private readonly IValidator<UpdateCommentRequest> _updateValidator;

    public CommentService(
        IUnitOfWork unitOfWork,
        ICommentEngagementRepository commentEngagement,
        IValidator<CreateCommentRequest> createValidator,
        IValidator<UpdateCommentRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _comments = unitOfWork.GetRepository<Comment>();
        _articles = unitOfWork.GetRepository<Article>();
        _users = unitOfWork.GetRepository<AppUser>();
        _notifications = unitOfWork.GetRepository<Notification>();
        _commentEngagement = commentEngagement;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PaginationResponse<CommentResponse>>> GetArticleCommentsAsync(
        Guid articleId,
        int pageNumber = 1,
        int pageSize = 10,
        Guid? viewerId = null,
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

        return Result<PaginationResponse<CommentResponse>>.Success(await MapPageAsync(page, viewerId, cancellationToken));
    }

    public async Task<Result<PaginationResponse<CommentResponse>>> GetRepliesAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 10,
        Guid? viewerId = null,
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

        return Result<PaginationResponse<CommentResponse>>.Success(await MapPageAsync(page, viewerId, cancellationToken));
    }

    public async Task<Result<CommentResponse>> GetByIdAsync(Guid commentId, Guid? viewerId = null, CancellationToken cancellationToken = default)
    {
        var comment = await _comments.GetSingleAsync(
            item => item.CommentId == commentId,
            disableTracking: true,
            ignoreQueryFilter: true,
            cancellationToken: cancellationToken);
        if (comment is null || !await IsPublishedArticleAsync(comment.ArticleId, cancellationToken))
            return Result<CommentResponse>.Failure(CommentErrors.CommentNotFound);

        var response = await MapCommentsAsync([comment], viewerId, cancellationToken);
        return Result<CommentResponse>.Success(response[0]);
    }

    public Task<Result<CommentResponse>> LikeAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default)
    {
        return ChangeLikeAsync(userId, commentId, isLiked: true, cancellationToken);
    }

    public Task<Result<CommentResponse>> UnlikeAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default)
    {
        return ChangeLikeAsync(userId, commentId, isLiked: false, cancellationToken);
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

        var article = await _articles.GetSingleAsync(
            item => item.ArticleId == articleId && item.Status == ArticleStatus.Published,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (article is null)
            return Result<CommentResponse>.Failure(CommentErrors.ArticleNotFound);

        var author = await _users.GetSingleAsync(
            user => user.Id == userId && user.IsActive && !user.IsDeleted,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (author is null)
            return Result<CommentResponse>.Failure(CommentErrors.AuthorNotFound);

        var notificationReceiverId = article.UserId;
        if (request.ParentCommentId is Guid parentCommentId)
        {
            var parent = await _comments.GetSingleAsync(
                comment => comment.CommentId == parentCommentId && !comment.IsDeleted,
                disableTracking: true,
                ignoreQueryFilter: true,
                cancellationToken: cancellationToken);
            if (parent is null || parent.ArticleId != articleId)
                return Result<CommentResponse>.Failure(CommentErrors.ParentCommentNotFound);

            notificationReceiverId = parent.UserId;
        }

        var comment = new Comment
        {
            CommentId = Guid.NewGuid(),
            UserId = userId,
            ArticleId = articleId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim()
        };

        if (notificationReceiverId != userId)
        {
            var isReply = request.ParentCommentId.HasValue;
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = isReply ? NotificationType.NewReply : NotificationType.NewComment,
                Content = isReply ? $"{author.DisplayName} replied to your comment." : $"{author.DisplayName} commented on your article.",
                ReceiverUserId = notificationReceiverId,
                ActorUserId = userId,
                ArticleId = articleId,
                CommentId = comment.CommentId
            };

            if (!await _notifications.AddAsync(notification, cancellationToken))
                return Result<CommentResponse>.Failure(CommentErrors.SaveFailed);
        }

        if (!await _comments.AddAsync(comment, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<CommentResponse>.Failure(CommentErrors.SaveFailed);

        var response = await MapCommentsAsync([comment], userId, cancellationToken);
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

        var response = await MapCommentsAsync([comment], userId, cancellationToken);
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
        Guid? viewerId,
        CancellationToken cancellationToken)
    {
        var items = await MapCommentsAsync(page.Items, viewerId, cancellationToken);
        return new PaginationResponse<CommentResponse>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    private async Task<IReadOnlyList<CommentResponse>> MapCommentsAsync(
        IReadOnlyCollection<Comment> comments,
        Guid? viewerId,
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
        var engagement = await _commentEngagement.GetSnapshotsAsync(
            comments.Select(comment => comment.CommentId).ToArray(), viewerId, cancellationToken);
        var responses = new List<CommentResponse>(comments.Count);

        foreach (var comment in comments)
        {
            var commentEngagement = engagement.GetValueOrDefault(comment.CommentId);
            var hasActiveAuthor = userLookup.TryGetValue(comment.UserId, out var author)
                && author.IsActive
                && !author.IsDeleted;
            var displayName = hasActiveAuthor ? author!.DisplayName : "Deleted user";
            responses.Add(CommentMappingConfig.ToResponse(
                comment,
                displayName,
                commentEngagement?.ReplyCount ?? 0,
                commentEngagement));
        }

        return responses;
    }

    private async Task<Result<CommentResponse>> ChangeLikeAsync(
        Guid userId,
        Guid commentId,
        bool isLiked,
        CancellationToken cancellationToken)
    {
        var comment = await _comments.GetSingleAsync(
            item => item.CommentId == commentId && item.Article.Status == ArticleStatus.Published,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (comment is null)
            return Result<CommentResponse>.Failure(CommentErrors.CommentNotFound);

        var actor = await _users.GetSingleAsync(
            user => user.Id == userId && user.IsActive && !user.IsDeleted,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (actor is null)
            return Result<CommentResponse>.Failure(CommentErrors.AuthorNotFound);

        if (isLiked)
        {
            var notification = comment.UserId == userId
                ? null
                : new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    NotificationType = NotificationType.CommentLiked,
                    Content = $"{actor.DisplayName} liked your comment.",
                    ReceiverUserId = comment.UserId,
                    ActorUserId = userId,
                    ArticleId = comment.ArticleId,
                    CommentId = comment.CommentId
                };
            try
            {
                await _commentEngagement.EnsureLikeAsync(commentId, userId, notification, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<CommentResponse>.Failure(CommentErrors.SaveFailed);
            }
        }
        else
        {
            try
            {
                await _commentEngagement.RemoveLikeAsync(commentId, userId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<CommentResponse>.Failure(CommentErrors.DeleteFailed);
            }
        }

        var response = await MapCommentsAsync([comment], userId, cancellationToken);
        return Result<CommentResponse>.Success(response[0]);
    }
}