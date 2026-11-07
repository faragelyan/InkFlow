using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class FollowConfig : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(x => new { x.FollowerId, x.FollowingId });
        builder.Property(x => x.CreatedAt).IsRequired();

        // Reverse index for FollowingId queries (e.g. get followers of a user)
        builder.HasIndex(x => x.FollowingId);

        // Navigation properties
        builder.HasOne(x => x.Follower)
            .WithMany(u => u.Followings)
            .HasForeignKey(x => x.FollowerId);

        builder.HasOne(x => x.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(x => x.FollowingId);
    }
}
