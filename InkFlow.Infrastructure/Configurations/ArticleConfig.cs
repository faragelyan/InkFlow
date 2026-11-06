namespace InkFlow.Infrastructure;

public class ArticleConfig:IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(x => x.ArticleId);
        builder.Property(x => x.ArticleId).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.SubTitle).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.Property(x => x.CoverImageUrl).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.PublishedAt).IsRequired();
        builder.Property(x => x.ArchivedAt).IsRequired();
        builder.Property(x => x.AppUserId).IsRequired();

    }
}
