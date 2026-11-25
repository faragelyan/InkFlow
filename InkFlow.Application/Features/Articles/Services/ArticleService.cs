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
    private readonly IGenericRepository<Topic> _topics;
    private readonly IGenericRepository<Tag> _tags;
    private readonly IGenericRepository<ArticleTopic> _articleTopics;
    private readonly IGenericRepository<ArticleTag> _articleTags;
    private readonly IGenericRepository<ArticleBookmark> _bookmarks;
    private readonly IArticleEngagementRepository _engagement;
    private readonly IValidator<CreateArticleRequest> _createValidator;
    private readonly IValidator<UpdateArticleRequest> _updateValidator;

    public ArticleService(
        IUnitOfWork unitOfWork,
        IArticleEngagementRepository engagement,
        IValidator<CreateArticleRequest> createValidator,
        IValidator<UpdateArticleRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _users = unitOfWork.GetRepository<AppUser>();
        _articles = unitOfWork.GetRepository<Article>();
        _topics = unitOfWork.GetRepository<Topic>();
        _tags = unitOfWork.GetRepository<Tag>();
        _articleTopics = unitOfWork.GetRepository<ArticleTopic>();
        _articleTags = unitOfWork.GetRepository<ArticleTag>();
        _bookmarks = unitOfWork.GetRepository<ArticleBookmark>();
        _engagement = engagement;
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

        var taxonomyResult = await ResolveTaxonomyAsync(request.TopicIds ?? [], request.TagIds ?? [], cancellationToken);
        if (taxonomyResult.Error is not null)
            return Result<ArticleResponse>.Failure(taxonomyResult.Error);

        if (!await _articles.AddAsync(article, cancellationToken)
            || (taxonomyResult.Topics.Count > 0 && !await _articleTopics.AddRangeAsync(
                taxonomyResult.Topics.Select(topicId => new ArticleTopic { ArticleId = articleId, TopicId = topicId }), cancellationToken))
            || (taxonomyResult.Tags.Count > 0 && !await _articleTags.AddRangeAsync(
                taxonomyResult.Tags.Select(tagId => new ArticleTag { ArticleId = articleId, TagId = tagId }), cancellationToken)))
            return Result<ArticleResponse>.Failure(ArticleErrors.SaveFailed);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<ArticleResponse>.Failure(ArticleErrors.SaveFailed);

        await LoadTaxonomyAsync([article], cancellationToken);
        var engagement = await GetEngagementAsync([article], authorId, cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article, engagement[articleId]));
    }

    public async Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetPublishedAsync(
        Guid? authorId,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        string? topicSlug = null,
        string? tagSlug = null,
        Guid? viewerId = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = PaginationHelper.Normalize(pageNumber, pageSize, DefaultPageSize, MaximumPageSize);
        var normalizedTopicSlug = NormalizeOptionalText(topicSlug)?.ToLowerInvariant();
        var normalizedTagSlug = NormalizeOptionalText(tagSlug)?.ToLowerInvariant();
        var articles = await _articles.GetPagedAsync(
            pageNumber: normalizedPage,
            pageSize: normalizedSize,
            filter: article => article.Status == ArticleStatus.Published
                && (authorId == null || article.UserId == authorId.Value)
                && (normalizedTopicSlug == null || article.ArticleTopics.Any(link => link.Topic.Slug == normalizedTopicSlug))
                && (normalizedTagSlug == null || article.ArticleTags.Any(link => link.Tag.Slug == normalizedTagSlug)),
            orderBy: query => query.OrderByDescending(article => article.PublishedAt)
                .ThenByDescending(article => article.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        await LoadTaxonomyAsync(articles.Items, cancellationToken);
        var engagement = await GetEngagementAsync(articles.Items, viewerId, cancellationToken);
        return Result<PaginationResponse<ArticleSummaryResponse>>.Success(MapPage(articles, engagement));
    }

    public async Task<Result<ArticleResponse>> GetPublishedByIdAsync(
        Guid articleId,
        Guid? viewerId = null,
        CancellationToken cancellationToken = default)
    {
        var article = await _articles.GetSingleAsync(
            item => item.ArticleId == articleId && item.Status == ArticleStatus.Published,
            disableTracking: true,
            cancellationToken: cancellationToken);

        if (article is null)
            return Result<ArticleResponse>.Failure(ArticleErrors.ArticleNotFound);

        await LoadTaxonomyAsync([article], cancellationToken);
        var engagement = await GetEngagementAsync([article], viewerId, cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article, engagement[articleId]));
    }

    public async Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetMineAsync(
        Guid authorId,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = PaginationHelper.Normalize(pageNumber, pageSize, DefaultPageSize, MaximumPageSize);
        var articles = await _articles.GetPagedAsync(
            pageNumber: normalizedPage,
            pageSize: normalizedSize,
            filter: article => article.UserId == authorId,
            orderBy: query => query.OrderByDescending(article => article.UpdatedAt ?? article.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        await LoadTaxonomyAsync(articles.Items, cancellationToken);
        var engagement = await GetEngagementAsync(articles.Items, authorId, cancellationToken);
        return Result<PaginationResponse<ArticleSummaryResponse>>.Success(MapPage(articles, engagement));
    }

    public async Task<Result<ArticleResponse>> GetMineByIdAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var article = await GetOwnedArticleAsync(authorId, articleId, disableTracking: true, cancellationToken);
        if (article is null)
            return Result<ArticleResponse>.Failure(ArticleErrors.ArticleNotFound);

        await LoadTaxonomyAsync([article], cancellationToken);
        var engagement = await GetEngagementAsync([article], authorId, cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article, engagement[articleId]));
    }

    public async Task<Result<PaginationResponse<ArticleSummaryResponse>>> GetMyBookmarksAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize, DefaultPageSize, MaximumPageSize);
        var bookmarks = await _bookmarks.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: bookmark => bookmark.UserId == userId && bookmark.Article.Status == ArticleStatus.Published,
            orderBy: query => query.OrderByDescending(bookmark => bookmark.CreatedAt),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var articleIds = bookmarks.Items.Select(bookmark => bookmark.ArticleId).ToList();
        var articles = articleIds.Count == 0
            ? []
            : await _articles.GetAllAsync(
                article => articleIds.Contains(article.ArticleId) && article.Status == ArticleStatus.Published,
                disableTracking: true,
                cancellationToken: cancellationToken);
        var articleLookup = articles.ToDictionary(article => article.ArticleId);
        var orderedArticles = articleIds
            .Where(articleLookup.ContainsKey)
            .Select(articleId => articleLookup[articleId])
            .ToList();

        await LoadTaxonomyAsync(orderedArticles, cancellationToken);
        var engagement = await GetEngagementAsync(orderedArticles, userId, cancellationToken);
        var items = orderedArticles
            .Select(article => ArticleMappingConfig.ToSummaryResponse(article, engagement[article.ArticleId]))
            .ToList();

        return Result<PaginationResponse<ArticleSummaryResponse>>.Success(
            new PaginationResponse<ArticleSummaryResponse>(items, bookmarks.TotalCount, bookmarks.PageNumber, bookmarks.PageSize));
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

        var taxonomyResult = await ResolveTaxonomyAsync(request.TopicIds ?? [], request.TagIds ?? [], cancellationToken);
        if (taxonomyResult.Error is not null)
            return Result<ArticleResponse>.Failure(taxonomyResult.Error);

        var existingTopics = await _articleTopics.GetAllAsync(link => link.ArticleId == articleId, cancellationToken: cancellationToken);
        var existingTags = await _articleTags.GetAllAsync(link => link.ArticleId == articleId, cancellationToken: cancellationToken);

        var requestedTopicIds = taxonomyResult.Topics.ToHashSet();
        var requestedTagIds = taxonomyResult.Tags.ToHashSet();
        var removedTopics = existingTopics.Where(link => !requestedTopicIds.Contains(link.TopicId)).ToList();
        var removedTags = existingTags.Where(link => !requestedTagIds.Contains(link.TagId)).ToList();
        var existingTopicIds = existingTopics.Select(link => link.TopicId).ToHashSet();
        var existingTagIds = existingTags.Select(link => link.TagId).ToHashSet();
        var addedTopics = requestedTopicIds.Except(existingTopicIds)
            .Select(topicId => new ArticleTopic { ArticleId = articleId, TopicId = topicId }).ToList();
        var addedTags = requestedTagIds.Except(existingTagIds)
            .Select(tagId => new ArticleTag { ArticleId = articleId, TagId = tagId }).ToList();

        if ((removedTopics.Count > 0 && !await _articleTopics.DeleteRangeAsync(removedTopics, cancellationToken))
            || (removedTags.Count > 0 && !await _articleTags.DeleteRangeAsync(removedTags, cancellationToken))
            || (addedTopics.Count > 0 && !await _articleTopics.AddRangeAsync(addedTopics, cancellationToken))
            || (addedTags.Count > 0 && !await _articleTags.AddRangeAsync(addedTags, cancellationToken)))
            return Result<ArticleResponse>.Failure(ArticleErrors.UpdateFailed);

        if (!await _articles.UpdateAsync(article, cancellationToken))
            return Result<ArticleResponse>.Failure(ArticleErrors.UpdateFailed);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<ArticleResponse>.Failure(ArticleErrors.UpdateFailed);

        await LoadTaxonomyAsync([article], cancellationToken);
        var engagement = await GetEngagementAsync([article], authorId, cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article, engagement[articleId]));
    }

    public Task<Result<ArticleEngagementResponse>> LikeAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default)
    {
        return ChangeEngagementAsync(userId, articleId, isLike: true, isEnabled: true, cancellationToken);
    }

    public Task<Result<ArticleEngagementResponse>> UnlikeAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default)
    {
        return ChangeEngagementAsync(userId, articleId, isLike: true, isEnabled: false, cancellationToken);
    }

    public Task<Result<ArticleEngagementResponse>> BookmarkAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default)
    {
        return ChangeEngagementAsync(userId, articleId, isLike: false, isEnabled: true, cancellationToken);
    }

    public Task<Result<ArticleEngagementResponse>> RemoveBookmarkAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default)
    {
        return ChangeEngagementAsync(userId, articleId, isLike: false, isEnabled: false, cancellationToken);
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

    public Task<Result<ArticleResponse>> UnarchiveAsync(
        Guid authorId,
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(authorId, articleId, ArticleStatus.Published, cancellationToken);
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
        {
            await LoadTaxonomyAsync([article], cancellationToken);
            var currentEngagement = await GetEngagementAsync([article], authorId, cancellationToken);
            return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article, currentEngagement[articleId]));
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

        if (await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<ArticleResponse>.Failure(ArticleErrors.UpdateFailed);

        await LoadTaxonomyAsync([article], cancellationToken);
        var engagement = await GetEngagementAsync([article], authorId, cancellationToken);
        return Result<ArticleResponse>.Success(ArticleMappingConfig.ToResponse(article, engagement[articleId]));
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
        PaginationResponse<Article> articles,
        IReadOnlyDictionary<Guid, ArticleEngagementSnapshot> engagement)
    {
        return new PaginationResponse<ArticleSummaryResponse>(
            articles.Items.Select(article => ArticleMappingConfig.ToSummaryResponse(article, engagement[article.ArticleId])).ToList(),
            articles.TotalCount,
            articles.PageNumber,
            articles.PageSize);
    }

    private async Task<Dictionary<Guid, ArticleEngagementSnapshot>> GetEngagementAsync(
        IReadOnlyCollection<Article> articles,
        Guid? viewerId,
        CancellationToken cancellationToken)
    {
        var snapshots = await _engagement.GetSnapshotsAsync(
            articles.Select(article => article.ArticleId).ToArray(), viewerId, cancellationToken);
        return articles.ToDictionary(
            article => article.ArticleId,
            article => snapshots.GetValueOrDefault(article.ArticleId) ?? new ArticleEngagementSnapshot());
    }

    private async Task<Result<ArticleEngagementResponse>> ChangeEngagementAsync(
        Guid userId,
        Guid articleId,
        bool isLike,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        var targetArticle = await _articles.GetSingleAsync(
            article => article.ArticleId == articleId && article.Status == ArticleStatus.Published,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (targetArticle is null)
            return Result<ArticleEngagementResponse>.Failure(ArticleErrors.ArticleNotFound);

        if (isLike)
        {
            if (isEnabled)
            {
                var notification = targetArticle.UserId == userId
                    ? null
                    : new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        NotificationType = NotificationType.ArticleLiked,
                        Content = "Someone liked your article.",
                        ReceiverUserId = targetArticle.UserId,
                        ActorUserId = userId,
                        ArticleId = articleId
                    };
                try
                {
                    await _engagement.EnsureLikeAsync(articleId, userId, notification, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    return Result<ArticleEngagementResponse>.Failure(ArticleErrors.SaveFailed);
                }
            }
            else
            {
                try
                {
                    await _engagement.RemoveLikeAsync(articleId, userId, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    return Result<ArticleEngagementResponse>.Failure(ArticleErrors.UpdateFailed);
                }
            }
        }
        else
        {
            try
            {
                if (isEnabled)
                    await _engagement.EnsureBookmarkAsync(articleId, userId, cancellationToken);
                else
                    await _engagement.RemoveBookmarkAsync(articleId, userId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result<ArticleEngagementResponse>.Failure(ArticleErrors.UpdateFailed);
            }
        }

        var snapshot = (await _engagement.GetSnapshotsAsync([articleId], userId, cancellationToken))[articleId];
        return Result<ArticleEngagementResponse>.Success(new ArticleEngagementResponse
        {
            ArticleId = articleId,
            LikeCount = snapshot.LikeCount,
            BookmarkCount = snapshot.BookmarkCount,
            IsLiked = snapshot.IsLiked,
            IsBookmarked = snapshot.IsBookmarked
        });
    }

    private async Task LoadTaxonomyAsync(IReadOnlyCollection<Article> articles, CancellationToken cancellationToken)
    {
        if (articles.Count == 0)
            return;

        var articleIds = articles.Select(article => article.ArticleId).ToList();
        var topicLinks = await _articleTopics.GetAllAsync(
            link => articleIds.Contains(link.ArticleId), cancellationToken: cancellationToken);
        var tagLinks = await _articleTags.GetAllAsync(
            link => articleIds.Contains(link.ArticleId), cancellationToken: cancellationToken);
        var topicIds = topicLinks.Select(link => link.TopicId).Distinct().ToList();
        var tagIds = tagLinks.Select(link => link.TagId).Distinct().ToList();
        var topics = topicIds.Count == 0
            ? []
            : await _topics.GetAllAsync(topic => topicIds.Contains(topic.TopicId), disableTracking: true, cancellationToken: cancellationToken);
        var tags = tagIds.Count == 0
            ? []
            : await _tags.GetAllAsync(tag => tagIds.Contains(tag.TagId), disableTracking: true, cancellationToken: cancellationToken);
        var topicLookup = topics.ToDictionary(topic => topic.TopicId);
        var tagLookup = tags.ToDictionary(tag => tag.TagId);
        foreach (var article in articles)
        {
            article.ArticleTopics = topicLinks
                .Where(link => link.ArticleId == article.ArticleId && topicLookup.ContainsKey(link.TopicId))
                .Select(link =>
                {
                    link.Topic = topicLookup[link.TopicId];
                    return link;
                }).ToList();
            article.ArticleTags = tagLinks
                .Where(link => link.ArticleId == article.ArticleId && tagLookup.ContainsKey(link.TagId))
                .Select(link =>
                {
                    link.Tag = tagLookup[link.TagId];
                    return link;
                }).ToList();
        }
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

    private async Task<(IReadOnlyList<Guid> Topics, IReadOnlyList<Guid> Tags, string? Error)> ResolveTaxonomyAsync(
        IReadOnlyList<Guid> topicIds,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        var distinctTopicIds = topicIds.Distinct().ToList();
        var distinctTagIds = tagIds.Distinct().ToList();

        if (distinctTopicIds.Count > 3)
            return ([], [], ArticleErrors.TooManyTopics);
        if (distinctTagIds.Count > 10)
            return ([], [], ArticleErrors.TooManyTags);

        var existingTopics = distinctTopicIds.Count == 0
            ? []
            : await _topics.GetAllAsync(topic => distinctTopicIds.Contains(topic.TopicId), disableTracking: true, cancellationToken: cancellationToken);
        if (existingTopics.Count != distinctTopicIds.Count)
            return ([], [], ArticleErrors.InvalidTopics);

        var existingTags = distinctTagIds.Count == 0
            ? []
            : await _tags.GetAllAsync(tag => distinctTagIds.Contains(tag.TagId), disableTracking: true, cancellationToken: cancellationToken);
        if (existingTags.Count != distinctTagIds.Count)
            return ([], [], ArticleErrors.InvalidTags);

        return (distinctTopicIds, distinctTagIds, null);
    }
}