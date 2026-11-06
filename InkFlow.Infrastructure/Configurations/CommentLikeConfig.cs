namespace InkFlow.Infrastructure;

public class CommentLikeConfig:IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.HasKey(x => new { x.UserId, x.CommentId });
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
