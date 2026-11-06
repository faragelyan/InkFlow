namespace InkFlow.Infrastructure;

public class ArticleTopicConfig:IEntityTypeConfiguration<ArticleTopic>
{
    public void Configure(EntityTypeBuilder<ArticleTopic> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.TopicId });
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
