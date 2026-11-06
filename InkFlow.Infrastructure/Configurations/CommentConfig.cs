namespace InkFlow.Infrastructure;

public class CommentConfig:IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(x => x.CommentId);
        builder.Property(x => x.CommentId).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ArticleId).IsRequired();
        builder.Property(x => x.ParentCommentId).IsRequired();
    }
}
