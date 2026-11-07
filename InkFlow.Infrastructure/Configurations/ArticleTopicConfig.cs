using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class ArticleTopicConfig : IEntityTypeConfiguration<ArticleTopic>
{
    public void Configure(EntityTypeBuilder<ArticleTopic> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.TopicId });

        // Reverse index for TopicId queries (e.g. get all articles in this TopicId)
        builder.HasIndex(x => x.TopicId);

        // Navigation properties
        builder.HasOne(x => x.Article)
            .WithMany(a => a.ArticleTopics)
            .HasForeignKey(x => x.ArticleId);

        builder.HasOne(x => x.Topic)
            .WithMany(t => t.ArticleTopics)
            .HasForeignKey(x => x.TopicId);
    }
}
