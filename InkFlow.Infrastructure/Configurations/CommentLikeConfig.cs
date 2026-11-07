using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class CommentLikeConfig : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.HasKey(x => new { x.UserId, x.CommentId });
        builder.Property(x => x.CreatedAt).IsRequired();

        // Reverse index for CommentId queries (e.g. comment like count and likers)
        builder.HasIndex(x => x.CommentId);

        // Navigation properties
        builder.HasOne(x => x.User)
            .WithMany(u => u.CommentLikes)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Comment)
            .WithMany(c => c.CommentLikes)
            .HasForeignKey(x => x.CommentId);
    }
}
