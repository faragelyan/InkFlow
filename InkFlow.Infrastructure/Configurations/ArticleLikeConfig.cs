using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class ArticleLikeConfig : IEntityTypeConfiguration<ArticleLike>
{
    public void Configure(EntityTypeBuilder<ArticleLike> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ArticleId });
        builder.Property(x => x.CreatedAt).IsRequired();

        // Reverse index for ArticleId queries (e.g. article like count and likers)
        builder.HasIndex(x => x.ArticleId);

        // Navigation properties
        builder.HasOne(x => x.User)
            .WithMany(u => u.ArticleLikes)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.ArticleLikes)
            .HasForeignKey(x => x.ArticleId);
    }
}
