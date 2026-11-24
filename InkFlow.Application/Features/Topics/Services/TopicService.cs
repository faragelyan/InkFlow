using System.Globalization;
using System.Text;
using FluentValidation;
using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Topics.DTOs.Requests;
using InkFlow.Application.Features.Topics.DTOs.Responses;
using InkFlow.Application.Features.Topics.Interfaces;
using InkFlow.Application.Features.Topics.Mappings;
using InkFlow.Application.Interfaces;
using InkFlow.Application.Interfaces.Repositories;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Topics.Services;

public class TopicService : ITopicService
{
    private const int MaximumPageSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Topic> _topics;
    private readonly IGenericRepository<ArticleTopic> _articleTopics;
    private readonly IValidator<CreateTopicRequest> _createValidator;
    private readonly IValidator<UpdateTopicRequest> _updateValidator;

    public TopicService(
        IUnitOfWork unitOfWork,
        IValidator<CreateTopicRequest> createValidator,
        IValidator<UpdateTopicRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _topics = unitOfWork.GetRepository<Topic>();
        _articleTopics = unitOfWork.GetRepository<ArticleTopic>();
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PaginationResponse<TopicResponse>>> GetAsync(
        string? search,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaximumPageSize);

        var normalizedSearch = search?.Trim();
        var page = await _topics.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: topic => string.IsNullOrWhiteSpace(normalizedSearch)
                || topic.Name.Contains(normalizedSearch),
            orderBy: query => query.OrderBy(topic => topic.Name),
            disableTracking: true,
            cancellationToken: cancellationToken);

        var items = new List<TopicResponse>(page.Items.Count);
        foreach (var topic in page.Items)
        {
            var count = await _articleTopics.CountAsync(
                link => link.TopicId == topic.TopicId,
                cancellationToken: cancellationToken);
            items.Add(TopicMappingConfig.ToResponse(topic, count));
        }

        return Result<PaginationResponse<TopicResponse>>.Success(
            new PaginationResponse<TopicResponse>(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Result<TopicResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var topic = await _topics.GetSingleAsync(
            item => item.Slug == normalizedSlug,
            disableTracking: true,
            cancellationToken: cancellationToken);
        if (topic is null)
            return Result<TopicResponse>.Failure(TopicErrors.TopicNotFound);

        var count = await _articleTopics.CountAsync(link => link.TopicId == topic.TopicId, cancellationToken: cancellationToken);
        return Result<TopicResponse>.Success(TopicMappingConfig.ToResponse(topic, count));
    }

    public async Task<Result<TopicResponse>> CreateAsync(CreateTopicRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<TopicResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var name = request.Name.Trim();
        if (await _topics.ExistsAsync(topic => topic.Name.ToLower() == name.ToLower(), cancellationToken: cancellationToken))
            return Result<TopicResponse>.Failure(TopicErrors.DuplicateName);

        var topic = new Topic
        {
            TopicId = Guid.NewGuid(),
            Name = name,
            Slug = CreateUniqueBaseSlug(name)
        };

        if (await _topics.ExistsAsync(item => item.Slug == topic.Slug, cancellationToken: cancellationToken))
            return Result<TopicResponse>.Failure(TopicErrors.DuplicateSlug);

        if (!await _topics.AddAsync(topic, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<TopicResponse>.Failure(TopicErrors.SaveFailed);

        return Result<TopicResponse>.Success(TopicMappingConfig.ToResponse(topic, 0));
    }

    public async Task<Result<TopicResponse>> UpdateAsync(Guid topicId, UpdateTopicRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<TopicResponse>.Failure(validation.Errors.Select(error => error.ErrorMessage));

        var topic = await _topics.GetSingleAsync(item => item.TopicId == topicId, cancellationToken: cancellationToken);
        if (topic is null)
            return Result<TopicResponse>.Failure(TopicErrors.TopicNotFound);

        var name = request.Name.Trim();
        if (await _topics.ExistsAsync(item => item.TopicId != topicId && item.Name.ToLower() == name.ToLower(), cancellationToken: cancellationToken))
            return Result<TopicResponse>.Failure(TopicErrors.DuplicateName);

        topic.Name = name;
        topic.Slug = CreateUniqueBaseSlug(name);

        if (await _topics.ExistsAsync(item => item.TopicId != topicId && item.Slug == topic.Slug, cancellationToken: cancellationToken))
            return Result<TopicResponse>.Failure(TopicErrors.DuplicateSlug);

        if (!await _topics.UpdateAsync(topic, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result<TopicResponse>.Failure(TopicErrors.UpdateFailed);

        var count = await _articleTopics.CountAsync(link => link.TopicId == topic.TopicId, cancellationToken: cancellationToken);
        return Result<TopicResponse>.Success(TopicMappingConfig.ToResponse(topic, count));
    }

    public async Task<Result> DeleteAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        var topic = await _topics.GetSingleAsync(item => item.TopicId == topicId, cancellationToken: cancellationToken);
        if (topic is null)
            return Result.Failure(TopicErrors.TopicNotFound);

        if (await _articleTopics.ExistsAsync(link => link.TopicId == topicId, cancellationToken: cancellationToken))
            return Result.Failure(TopicErrors.TopicInUse);

        if (!await _topics.DeleteAsync(topic, cancellationToken)
            || await _unitOfWork.SaveChangesAsync(cancellationToken) < 1)
            return Result.Failure(TopicErrors.DeleteFailed);

        return Result.Success();
    }

    private static string CreateUniqueBaseSlug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "topic" : slug[..Math.Min(slug.Length, 100)].TrimEnd('-');
    }
}
