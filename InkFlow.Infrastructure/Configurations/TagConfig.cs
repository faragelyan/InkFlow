namespace InkFlow.Infrastructure;

public class TagConfig:IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(x => x.TagId);
        builder.Property(x => x.TagId).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
