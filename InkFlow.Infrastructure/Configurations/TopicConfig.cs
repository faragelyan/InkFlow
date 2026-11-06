namespace InkFlow.Infrastructure;

public class TopicConfig:IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(x => x.TopicId);
        builder.Property(x => x.TopicId).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
