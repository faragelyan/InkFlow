using InkFlow.Application.Features.Comments.DTOs.Responses;
using InkFlow.Application.Common.Models;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Comments.Mappings;

public static class CommentMappingConfig
{
    public static CommentResponse ToResponse(
        Comment comment,
        string authorDisplayName,
        int replyCount,
        CommentEngagementSnapshot? engagement = null)
    {
        return new CommentResponse
        {
            CommentId = comment.CommentId,
            ArticleId = comment.ArticleId,
            ParentCommentId = comment.ParentCommentId,
            AuthorId = comment.UserId,
            AuthorDisplayName = comment.IsDeleted ? "Deleted user" : authorDisplayName,
            Content = comment.IsDeleted ? null : comment.Content,
            IsDeleted = comment.IsDeleted,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            ReplyCount = replyCount,
            LikeCount = engagement?.LikeCount ?? 0,
            IsLiked = engagement?.IsLiked ?? false
        };
    }
}