using InkFlow.Application.Features.Tags.DTOs.Responses;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Tags.Mappings;

public static class TagMappingConfig
{
    public static TagSummaryResponse ToSummaryResponse(Tag tag)
    {
        return new TagSummaryResponse { TagId = tag.TagId, Name = tag.Name, Slug = tag.Slug };
    }

    public static TagResponse ToResponse(Tag tag, int articleCount)
    {
        return new TagResponse
        {
            TagId = tag.TagId,
            Name = tag.Name,
            Slug = tag.Slug,
            CreatedAt = tag.CreatedAt,
            ArticleCount = articleCount
        };
    }
}
