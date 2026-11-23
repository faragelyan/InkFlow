using FluentValidation;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Articles.DTOs.Requests;
using InkFlow.Application.Features.Articles.DTOs.Responses;
using InkFlow.Application.Features.Articles.Interfaces;
using InkFlow.Application.Features.Articles.Mappings;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Articles.Services;

public class ArticleService : IArticleService
{
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<AppUser> _users;
    private readonly IGenericRepository<Article> _articles;
    private readonly IValidator<CreateArticleRequest> _createValidator;
    private readonly IValidator<UpdateArticleRequest> _updateValidator;

    public ArticleService(
        IUnitOfWork unitOfWork,
        IValidator<CreateArticleRequest> createValidator,
        IValidator<UpdateArticleRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _users = unitOfWork.GetRepository<AppUser>();
        _articles = unitOfWork.GetRepository<Article>();
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<ArticleResponse>> CreateAsync(
        Guid authorId,
        CreateArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<ArticleResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var author = await _users.GetSingleAsync(
            user => user.Id == authorId,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (author is null || !author.IsActive)
            return Result<ArticleResponse>.Failure(ArticleErrors.AuthorNotFound);

        var articleId = Guid.NewGuid();
        var article = new Article
        {
            ArticleId = articleId,
            UserId = authorId,
            Title = request.Title.Trim(),
            Subtitle = NormalizeOptionalText(request.Subtitle),
            Content = request.Content.Trim(),
            Slug = CreateSlug(request.Title, articleId),
            CoverImageUrl = NormalizeOptionalText(request.CoverImageUrl),
            Status = ArticleStatus.Draft
        };

        if (!await _articles.AddAsync(article, cancellationToken))
            return Result<ArticleResponse>.Failure(ArticleErrors.SaveFailed);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article));
    }

    public async Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetPublishedAsync(
        Guid? authorId,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = NormalizePaging(pageNumber, pageSize);
        var articles = await _articles.GetPagedAsync(
            pageNumber: normalizedPage,
            pageSize: normalizedSize,
            filter: article => article.Status == ArticleStatus.Published
                && (authorId == null || article.UserId == authorId.Value),
            orderBy: query => query.OrderByDescending(article => article.PublishedAt)
                .ThenByDescending(article => article.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        return Result<PaginationResponse<ArticleSummaryResponse>>.Success(
            MapPage(articles));
    }

    public async Task<Result<ArticleResponse>> GetPublishedByIdAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var article = await _articles.GetSingleAsync(
            item => item.ArticleId == articleId && item.Status == ArticleStatus.Published,
            disableTracking: true,
            cancellationToken: cancellationToken);

        return article is null
            ? Result<ArticleResponse>.Failure(ArticleErrors.ArticleNotFound)
            : Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article));
    }

    public async Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetMineAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = NormalizePaging(pageNumber, pageSize);
        var articles = await _articles.GetPagedAsync(
            pageNumber: normalizedPage,
            pageSize: normalizedSize,
            filter: article => article.UserId == authorId,
            orderBy: query => query.OrderByDescending(article => article.UpdatedAt ?? article.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        return Result<PaginationResponse<ArticleSummaryResponse>>.Success(
            MapPage(articles));
    }

    public async Task<Result<ArticleResponse>> GetMineByIdAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var article = await GetOwnedArticleAsync(authorId, articleId, disableTracking: true, cancellationToken);
        return article is null
            ? Result<ArticleResponse>.Failure(ArticleErrors.ArticleNotFound)
            : Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article));
    }

    public async Task<Result<ArticleResponse>> UpdateAsync(
        Guid authorId,
        Guid articleId,
        UpdateArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<ArticleResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var article = await GetOwnedArticleAsync(authorId, articleId, disableTracking: false, cancellationToken);
        if (article is null)
            return Result<ArticleResponse>.Failure(ArticleErrors.ArticleNotFound);
        if (article.Status == ArticleStatus.Archived)
            return Result<ArticleResponse>.Failure(ArticleErrors.ArticleUnavailable);

        article.Title = request.Title.Trim();
        article.Subtitle = NormalizeOptionalText(request.Subtitle);
        article.Content = request.Content.Trim();
        article.CoverImageUrl = NormalizeOptionalText(request.CoverImageUrl);

        if (!await _articles.UpdateAsync(article, cancellationToken))
            return Result<ArticleResponse>.Failure(ArticleErrors.UpdateFailed);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article));
    }

    public Task<Result<ArticleResponse>> PublishAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(authorId, articleId, ArticleStatus.Published, cancellationToken);
    }

    public Task<Result<ArticleResponse>> UnpublishAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(authorId, articleId, ArticleStatus.Draft, cancellationToken);
    }

    public Task<Result<ArticleResponse>> ArchiveAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(authorId, articleId, ArticleStatus.Archived, cancellationToken);
    }

    private async Task<Result<ArticleResponse>> ChangeStatusAsync(
        Guid authorId,
        Guid articleId,
        ArticleStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var article = await GetOwnedArticleAsync(authorId, articleId, disableTracking: false, cancellationToken);
        if (article is null)
            return Result<ArticleResponse>.Failure(ArticleErrors.ArticleNotFound);

        if (article.Status == targetStatus)
            return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article));

        if (article.Status == ArticleStatus.Archived && targetStatus != ArticleStatus.Archived)
        {
            var error = targetStatus == ArticleStatus.Published
                ? ArticleErrors.CannotPublishArchived
                : ArticleErrors.CannotUnpublishArchived;
            return Result<ArticleResponse>.Failure(error);
        }

        if (targetStatus == ArticleStatus.Published)
        {
            if (string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Content))
                return Result<ArticleResponse>.Failure("Title and content are required to publish an article.");

            article.PublishedAt = DateTime.UtcNow;
            article.ArchivedAt = null;
        }
        else if (targetStatus == ArticleStatus.Draft)
        {
            article.PublishedAt = null;
        }
        else
        {
            article.ArchivedAt = DateTime.UtcNow;
        }

        article.Status = targetStatus;
        if (!await _articles.UpdateAsync(article, cancellationToken))
            return Result<ArticleResponse>.Failure(ArticleErrors.UpdateFailed);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article));
    }

    private Task<Article?> GetOwnedArticleAsync(
        Guid authorId,
        Guid articleId,
        bool disableTracking,
        CancellationToken cancellationToken)
    {
        return _articles.GetSingleAsync(
            article => article.ArticleId == articleId && article.UserId == authorId,
            disableTracking: disableTracking,
            cancellationToken: cancellationToken);
    }

    private static PaginationResponse<ArticleSummaryResponse> MapPage(
        PaginationResponse<Article> articles)
    {
        return new PaginationResponse<ArticleSummaryResponse>(
            articles.Items.Select(ArticleMappingConfig.ToSummaryResponse).ToList(),
            articles.TotalCount,
            articles.PageNumber,
            articles.PageSize);
    }

    private static (int PageNumber, int PageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, MaximumPageSize);
        var maxPageNumber = (int)Math.Min(
            int.MaxValue,
            (long)int.MaxValue / pageSize + 1);
        pageNumber = Math.Clamp(pageNumber, 1, maxPageNumber);
        return (pageNumber, pageSize);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string CreateSlug(string title, Guid articleId)
    {
        var slug = new string(title.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        slug = slug.Trim('-');
        if (slug.Length > 217)
            slug = slug[..217].TrimEnd('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "article";

        return $"{slug}-{articleId:N}";
    }
}