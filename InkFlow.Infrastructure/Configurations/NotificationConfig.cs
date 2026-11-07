using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.NotificationId);
        builder.Property(x => x.NotificationType).IsRequired();
        builder.Property(x => x.Content).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.ReadAt);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ReceiverUserId).IsRequired();
        builder.Property(x => x.ActorUserId).IsRequired();
        builder.Property(x => x.ArticleId);
        builder.Property(x => x.CommentId);

        // Indexes
        builder.HasIndex(x => x.ReceiverUserId)
            .HasFilter("[IsRead] = 0");

        builder.HasIndex(x => new { x.ReceiverUserId, x.CreatedAt });

        // Navigation properties
        builder.HasOne(x => x.Receiver)
            .WithMany(u => u.Notifications)
            .HasForeignKey(x => x.ReceiverUserId);

        builder.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorUserId);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.Notifications)
            .HasForeignKey(x => x.ArticleId);

        builder.HasOne(x => x.Comment)
            .WithMany(c => c.Notifications)
            .HasForeignKey(x => x.CommentId);
    }
}
