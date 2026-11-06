namespace InkFlow.Infrastructure;

public class ArticleLikeConfig:IEntityTypeConfiguration<ArticleLike>
{
    public void Configure(EntityTypeBuilder<ArticleLike> builder)
    {
        builder.HasKey(x => new { x.AppUserId, x.ArticleId });
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
