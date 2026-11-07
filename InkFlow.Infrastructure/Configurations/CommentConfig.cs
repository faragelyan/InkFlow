using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class CommentConfig : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(x => x.CommentId);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ArticleId).IsRequired();
        builder.Property(x => x.ParentCommentId);

        // Indexes
        builder.HasIndex(x => new { x.ArticleId, x.CreatedAt })
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.ParentCommentId, x.CreatedAt })
            .HasFilter("[ParentCommentId] IS NOT NULL");

        // Navigation properties
        builder.HasOne(x => x.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.Comments)
            .HasForeignKey(x => x.ArticleId);

        builder.HasOne(x => x.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(x => x.ParentCommentId);
    }
}
