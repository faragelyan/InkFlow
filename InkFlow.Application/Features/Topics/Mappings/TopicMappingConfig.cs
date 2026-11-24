using InkFlow.Application.Features.Topics.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Topics.Mappings;

public static class TopicMappingConfig
{
    public static TopicSummaryResponse ToSummaryResponse(Topic topic)
    {
        return new TopicSummaryResponse
        {
            TopicId = topic.TopicId,
            Name = topic.Name,
            Slug = topic.Slug
        };
    }

    public static TopicResponse ToResponse(Topic topic, int articleCount)
    {
        return new TopicResponse
        {
            TopicId = topic.TopicId,
            Name = topic.Name,
            Slug = topic.Slug,
            CreatedAt = topic.CreatedAt,
            ArticleCount = articleCount
        };
    }
}
