namespace InkFlow.Infrastructure;

public class ArticleBookmarkConfig:IEntityTypeConfiguration<ArticleBookmark>
{
    public void Configure(EntityTypeBuilder<ArticleBookmark> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ArticleId });
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
