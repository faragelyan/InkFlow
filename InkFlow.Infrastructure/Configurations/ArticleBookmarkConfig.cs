using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class ArticleBookmarkConfig : IEntityTypeConfiguration<ArticleBookmark>
{
    public void Configure(EntityTypeBuilder<ArticleBookmark> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ArticleId });
        builder.Property(x => x.CreatedAt).IsRequired();

        // Reverse index for ArticleId queries (e.g. article bookmark count)
        builder.HasIndex(x => x.ArticleId);

        // Navigation properties
        builder.HasOne(x => x.User)
            .WithMany(u => u.ArticleBookmarks)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.ArticleBookmarks)
            .HasForeignKey(x => x.ArticleId);
    }
}
