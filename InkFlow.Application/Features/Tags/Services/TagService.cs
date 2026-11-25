using FluentValidation;
using InkFlow.Application.Common;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Tags.DTOs.Requests;
using InkFlow.Application.Features.Tags.DTOs.Responses;
using InkFlow.Application.Features.Tags.Interfaces;
using InkFlow.Application.Features.Tags.Mappings;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Tags.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Tag> _tags;
    private readonly IGenericRepository<ArticleTag> _articleTags;
    private readonly IValidator<CreateTagRequest> _createValidator;
    private readonly IValidator<UpdateTagRequest> _updateValidator;

    public TagService(IUnitOfWork unitOfWork, IValidator<CreateTagRequest> createValidator, IValidator<UpdateTagRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _tags = unitOfWork.GetRepository<Tag>();
        _articleTags = unitOfWork.GetRepository<ArticleTag>();
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PaginationResponse<TagResponse>>> GetAsync(string? search, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
        var normalizedSearch = search?.Trim();

        var page = await _tags.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: tag => string.IsNullOrWhiteSpace(normalizedSearch) || tag.Name.Contains(normalizedSearch),
            orderBy: query => query.OrderBy(tag => tag.Name),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var items = new List<TagResponse>(page.Items.Count);
        foreach (var tag in page.Items)
        {
            var count = await _articleTags.CountAsync(
                link => link.TagId == tag.TagId && link.Article.Status == ArticleStatus.Published,
                cancellationToken: cancellationToken);
            items.Add(TagMappingConfig.ToResponse(tag, count));
        }

        return Result<PaginationResponse<TagResponse>>.Success(
            new PaginationResponse<TagResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Result<TagResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var tag = await _tags.GetSingleAsync(item => item.Slug == normalizedSlug, disableTracking: true, cancellationToken: cancellationToken);
        if (tag is null)
            return Result<TagResponse>.Failure(TagErrors.TagNotFound);

        var count = await _articleTags.CountAsync(
            link => link.TagId == tag.TagId && link.Article.Status == ArticleStatus.Published,
            cancellationToken: cancellationToken);
        return Result<TagResponse>.Success(TagMappingConfig.ToResponse(tag, count));
    }

    public async Task<Result<TagResponse>> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<TagResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var name = request.Name.Trim();
        if (await _tags.ExistsAsync(tag => tag.Name.ToLower() == name.ToLower(), cancellationToken: cancellationToken))
            return Result<TagResponse>.Failure(TagErrors.DuplicateName);

        var tag = new Tag { TagId = Guid.NewGuid(), Name = name, Slug = SlugGenerator.Create(name, 50, "tag") };

        if (await _tags.ExistsAsync(item => item.Slug == tag.Slug, cancellationToken: cancellationToken))
            return Result<TagResponse>.Failure(TagErrors.DuplicateSlug);

        if (!await _tags.AddAsync(tag, cancellationToken) || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<TagResponse>.Failure(TagErrors.SaveFailed);

        return Result<TagResponse>.Success(TagMappingConfig.ToResponse(tag, 0));
    }

    public async Task<Result<TagResponse>> UpdateAsync(Guid tagId, UpdateTagRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<TagResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var tag = await _tags.GetSingleAsync(item => item.TagId == tagId, cancellationToken: cancellationToken);
        if (tag is null)
            return Result<TagResponse>.Failure(TagErrors.TagNotFound);

        var name = request.Name.Trim();
        if (await _tags.ExistsAsync(item => item.TagId != tagId && item.Name.ToLower() == name.ToLower(), cancellationToken: cancellationToken))
            return Result<TagResponse>.Failure(TagErrors.DuplicateName);

        var slug = SlugGenerator.Create(name, 50, "tag");
        if (await _tags.ExistsAsync(item => item.TagId != tagId && item.Slug == slug, cancellationToken: cancellationToken))
            return Result<TagResponse>.Failure(TagErrors.DuplicateSlug);

        tag.Name = name;
        tag.Slug = slug;

        if (!await _tags.UpdateAsync(tag, cancellationToken) || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<TagResponse>.Failure(TagErrors.UpdateFailed);

        var count = await _articleTags.CountAsync(link => link.TagId == tag.TagId, cancellationToken: cancellationToken);
        return Result<TagResponse>.Success(TagMappingConfig.ToResponse(tag, count));
    }

    public async Task<Result> DeleteAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await _tags.GetSingleAsync(item => item.TagId == tagId, cancellationToken: cancellationToken);
        if (tag is null)
            return Result.Failure(TagErrors.TagNotFound);

        if (await _articleTags.ExistsAsync(link => link.TagId == tagId, cancellationToken: cancellationToken))
            return Result.Failure(TagErrors.TagInUse);

        if (!await _tags.DeleteAsync(tag, cancellationToken) || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result.Failure(TagErrors.DeleteFailed);

        return Result.Success();
    }

}
