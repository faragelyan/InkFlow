namespace InkFlow.Infrastructure;

public class NotificationConfig:IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.NotificationId);
        builder.Property(x => x.NotificationId).IsRequired();
        builder.Property(x => x.NotificationType).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ReceiverUserId).IsRequired();
        builder.Property(x => x.ArticleId).IsRequired();
        builder.Property(x => x.CommentId).IsRequired();
    }
}
