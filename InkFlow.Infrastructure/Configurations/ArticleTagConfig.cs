namespace InkFlow.Infrastructure;

public class ArticleTagConfig:IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.TagId });
        builder.Property(x => x.CreatedAt).IsRequired();
    }
