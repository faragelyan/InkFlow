namespace InkFlow.Infrastructure;

public class FollowConfig:IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(x => new { x.FollowerId, x.FollowingId });
        builder.Property(x => x.CreatedAt).IsRequired();
    }
    }
}
