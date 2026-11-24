using InkFlow.Application.Common.Models;
using InkFlow.Application.Common.Results;
using InkFlow.Application.Features.Topics.DTOs.Requests;
using InkFlow.Application.Features.Topics.DTOs.Responses;

namespace InkFlow.Application.Features.Topics.Interfaces;

public interface ITopicService
{
    Task<Result<PaginationResponse<TopicResponse>>> GetAsync(string? search, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<TopicResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<TopicResponse>> CreateAsync(CreateTopicRequest request, CancellationToken cancellationToken = default);
    Task<Result<TopicResponse>> UpdateAsync(Guid topicId, UpdateTopicRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid topicId, CancellationToken cancellationToken = default);
}
