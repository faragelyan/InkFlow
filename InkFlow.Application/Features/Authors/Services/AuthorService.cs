using FluentValidation;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Authors.DTOs.Requests;
using InkFlow.Application.Features.Authors.DTOs.Responses;
using InkFlow.Application.Features.Authors.Interfaces;
using InkFlow.Application.Features.Authors.Mappings;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Authors.Services;

public class AuthorService : IAuthorService
{
    private readonly IGenericRepository<AppUser> _users;
    private readonly IGenericRepository<Article> _articles;
    private readonly IGenericRepository<Follow> _follows;
    private readonly IFollowRepository _followRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateAuthorProfileRequest> _updateProfileValidator;

    public AuthorService(
        IUnitOfWork unitOfWork,
        IFollowRepository followRepository,
        IValidator<UpdateAuthorProfileRequest> updateProfileValidator)
    {
        _unitOfWork = unitOfWork;
        _users = unitOfWork.GetRepository<AppUser>();
        _articles = unitOfWork.GetRepository<Article>();
        _follows = unitOfWork.GetRepository<Follow>();
        _followRepository = followRepository;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<Result<AuthorProfileResponse>> GetProfileAsync(
        Guid authorId,
        CancellationToken cancellationToken = default,
        Guid? viewerId = null)
    {
        var author = await GetAuthorAsync(authorId, disableTracking: true, cancellationToken);

        if (author is null || !author.IsActive)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.NotFound);

        return Result<AuthorProfileResponse>.Success(
            await MapProfileAsync(author, cancellationToken, viewerId));
    }

    public async Task<Result<AuthorProfileResponse>> UpdateProfileAsync(
        Guid authorId,
        UpdateAuthorProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthorProfileResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var author = await GetAuthorAsync(authorId, disableTracking: false, cancellationToken);

        if (author is null || !author.IsActive)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.NotFound);

        author.DisplayName = request.DisplayName.Trim();
        author.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        author.ProfileImageUrl = string.IsNullOrWhiteSpace(request.ProfileImageUrl)
            ? null
            : request.ProfileImageUrl.Trim();

        if (!await _users.UpdateAsync(author, cancellationToken))
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.ProfileUpdateFailed);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.ProfileUpdateFailed);

        return Result<AuthorProfileResponse>.Success(
            await MapProfileAsync(author, cancellationToken, authorId));
    }

    public async Task<Result<PaginationResponse<AuthorArticleResponse>>> GetPublishedArticlesAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var author = await GetAuthorAsync(authorId, disableTracking: true, cancellationToken);

        if (author is null || !author.IsActive)
            return Result<PaginationResponse<AuthorArticleResponse>>.Failure(AuthorErrors.NotFound);

        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

        var articles = await _articles.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            filter: article => article.UserId == authorId && article.Status == ArticleStatus.Published,
            orderBy: query => query.OrderByDescending(article => article.PublishedAt)
                .ThenByDescending(article => article.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var response = new PaginationResponse<AuthorArticleResponse>(
            articles.Items.Select(AuthorMappingConfig.ToArticleResponse).ToList(),
            articles.TotalCount,
            articles.PageNumber,
            articles.PageSize);

        return Result<PaginationResponse<AuthorArticleResponse>>.Success(response);
    }

    public async Task<Result<AuthorProfileResponse>> FollowAsync(
        Guid followerId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        if (followerId == authorId)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.CannotFollowSelf);

        var follower = await GetAuthorAsync(followerId, disableTracking: true, cancellationToken);
        var author = await GetAuthorAsync(authorId, disableTracking: true, cancellationToken);
        if (follower is null || !follower.IsActive || author is null || !author.IsActive)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.NotFound);

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            NotificationType = NotificationType.NewFollower,
            Content = $"{follower.DisplayName} started following you.",
            ReceiverUserId = authorId,
            ActorUserId = followerId
        };
        try
        {
            await _followRepository.EnsureFollowingAsync(followerId, authorId, notification, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.FollowUpdateFailed);
        }
        return Result<AuthorProfileResponse>.Success(await MapProfileAsync(author, cancellationToken, followerId));
    }

    public async Task<Result<AuthorProfileResponse>> UnfollowAsync(
        Guid followerId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        if (followerId == authorId)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.CannotFollowSelf);

        var follower = await GetAuthorAsync(followerId, disableTracking: true, cancellationToken);
        var author = await GetAuthorAsync(authorId, disableTracking: true, cancellationToken);
        if (follower is null || !follower.IsActive || author is null || !author.IsActive)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.NotFound);

        try
        {
            await _followRepository.RemoveFollowingAsync(followerId, authorId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.FollowUpdateFailed);
        }

        return Result<AuthorProfileResponse>.Success(await MapProfileAsync(author, cancellationToken, followerId));
    }

    public Task<Result<PaginationResponse<AuthorConnectionResponse>>> GetFollowersAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return GetConnectionsAsync(authorId, followers: true, pageNumber, pageSize, cancellationToken);
    }

    public Task<Result<PaginationResponse<AuthorConnectionResponse>>> GetFollowingAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return GetConnectionsAsync(authorId, followers: false, pageNumber, pageSize, cancellationToken);
    }

    private async Task<Result<PaginationResponse<AuthorConnectionResponse>>> GetConnectionsAsync(
        Guid authorId,
        bool followers,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var author = await GetAuthorAsync(authorId, disableTracking: true, cancellationToken);
        if (author is null || !author.IsActive)
            return Result<PaginationResponse<AuthorConnectionResponse>>.Failure(AuthorErrors.NotFound);

        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var page = followers
            ? await _follows.GetPagedAsync(
                pageNumber,
                pageSize,
                filter: follow => follow.FollowingId == authorId && follow.Follower.IsActive && !follow.Follower.IsDeleted,
                orderBy: query => query.OrderByDescending(follow => follow.CreatedAt),
                disableTracking: true,
                cancellationToken: cancellationToken)
            : await _follows.GetPagedAsync(
                pageNumber,
                pageSize,
                filter: follow => follow.FollowerId == authorId && follow.Following.IsActive && !follow.Following.IsDeleted,
                orderBy: query => query.OrderByDescending(follow => follow.CreatedAt),
                disableTracking: true,
                cancellationToken: cancellationToken);

        var connectionIds = page.Items
            .Select(follow => followers ? follow.FollowerId : follow.FollowingId)
            .ToList();
        var users = connectionIds.Count == 0
            ? []
            : await _users.GetAllAsync(
                user => connectionIds.Contains(user.Id) && user.IsActive && !user.IsDeleted,
                disableTracking: true,
                cancellationToken: cancellationToken);
        var userLookup = users.ToDictionary(user => user.Id);
        var items = page.Items
            .Where(follow => userLookup.ContainsKey(followers ? follow.FollowerId : follow.FollowingId))
            .Select(follow => AuthorMappingConfig.ToConnectionResponse(
                userLookup[followers ? follow.FollowerId : follow.FollowingId], follow.CreatedAt))
            .ToList();

        return Result<PaginationResponse<AuthorConnectionResponse>>.Success(
            new PaginationResponse<AuthorConnectionResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    private Task<AppUser?> GetAuthorAsync(
        Guid authorId,
        bool disableTracking,
        CancellationToken cancellationToken)
    {
        return _users.GetSingleAsync(
            user => user.Id == authorId && !user.IsDeleted,
            disableTracking: disableTracking,
            cancellationToken: cancellationToken);
    }

    private async Task<AuthorProfileResponse> MapProfileAsync(
        AppUser author,
        CancellationToken cancellationToken,
        Guid? viewerId = null)
    {
        var articleCount = await _articles.CountAsync(
            article => article.UserId == author.Id && article.Status == ArticleStatus.Published,
            cancellationToken: cancellationToken);
        var followerCount = await _follows.CountAsync(
            follow => follow.FollowingId == author.Id
                && follow.Follower.IsActive
                && !follow.Follower.IsDeleted,
            cancellationToken: cancellationToken);
        var followingCount = await _follows.CountAsync(
            follow => follow.FollowerId == author.Id
                && follow.Following.IsActive
                && !follow.Following.IsDeleted,
            cancellationToken: cancellationToken);
        var isFollowing = viewerId.HasValue && await _follows.ExistsAsync(
            follow => follow.FollowerId == viewerId.Value
                && follow.FollowingId == author.Id
                && follow.Follower.IsActive
                && !follow.Follower.IsDeleted,
            cancellationToken: cancellationToken);

        return AuthorMappingConfig.ToProfileResponse(author, articleCount, followerCount, followingCount, isFollowing);
    }
}