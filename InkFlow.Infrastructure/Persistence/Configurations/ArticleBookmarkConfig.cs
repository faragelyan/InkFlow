using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure.Persistence.Configurations;

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
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.ArticleBookmarks)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
