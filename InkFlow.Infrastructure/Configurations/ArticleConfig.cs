using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class ArticleConfig : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(x => x.ArticleId);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Subtitle).HasMaxLength(500);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(250);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.PublishedAt);
        builder.Property(x => x.ArchivedAt);
        builder.Property(x => x.UserId).IsRequired();

        // Indexes
        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasIndex(x => x.PublishedAt)
            .HasFilter("[Status] = 2 AND [PublishedAt] IS NOT NULL");

        builder.HasIndex(x => new { x.UserId, x.PublishedAt })
            .HasFilter("[Status] = 2");

        // Navigation properties
        builder.HasOne(x => x.User)
            .WithMany(x => x.Articles)
            .HasForeignKey(x => x.UserId);
    }
}
