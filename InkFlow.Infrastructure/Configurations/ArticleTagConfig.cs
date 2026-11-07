using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class ArticleTagConfig : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.TagId });

        // Reverse index for TagId queries (e.g. get all articles tagged with this TagId)
        builder.HasIndex(x => x.TagId);

        // Navigation properties
        builder.HasOne(x => x.Article)
            .WithMany(a => a.ArticleTags)
            .HasForeignKey(x => x.ArticleId);

        builder.HasOne(x => x.Tag)
            .WithMany(t => t.ArticleTags)
            .HasForeignKey(x => x.TagId);
    }
}
