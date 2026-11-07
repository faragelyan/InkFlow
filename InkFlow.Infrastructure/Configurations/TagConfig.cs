using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class TagConfig : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(x => x.TagId);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).IsRequired();

        // Unique index for slug lookups (e.g. /tags/{slug})
        builder.HasIndex(x => x.Slug)
            .IsUnique();
    }
}
