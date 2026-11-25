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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateAuthorProfileRequest> _updateProfileValidator;

    public AuthorService(
        IUnitOfWork unitOfWork,
        IValidator<UpdateAuthorProfileRequest> updateProfileValidator)
    {
        _unitOfWork = unitOfWork;
        _users = unitOfWork.GetRepository<AppUser>();
        _articles = unitOfWork.GetRepository<Article>();
        _follows = unitOfWork.GetRepository<Follow>();
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<Result<AuthorProfileResponse>> GetProfileAsync(
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var author = await GetAuthorAsync(authorId, disableTracking: true, cancellationToken);

        if (author is null || !author.IsActive)
            return Result<AuthorProfileResponse>.Failure(AuthorErrors.NotFound);

        return Result<AuthorProfileResponse>.Success(
            await MapProfileAsync(author, cancellationToken));
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
            await MapProfileAsync(author, cancellationToken));
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

    private Task<AppUser?> GetAuthorAsync(
        Guid authorId,
        bool disableTracking,
        CancellationToken cancellationToken)
    {
        return _users.GetSingleAsync(
            user => user.Id == authorId,
            disableTracking: disableTracking,
            cancellationToken: cancellationToken);
    }

    private async Task<AuthorProfileResponse> MapProfileAsync(
        AppUser author,
        CancellationToken cancellationToken)
    {
        var articleCount = await _articles.CountAsync(
            article => article.UserId == author.Id && article.Status == ArticleStatus.Published,
            cancellationToken: cancellationToken);
        var followerCount = await _follows.CountAsync(
            follow => follow.FollowingId == author.Id,
            cancellationToken: cancellationToken);

        return AuthorMappingConfig.ToProfileResponse(author, articleCount, followerCount);
    }
}