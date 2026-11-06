namespace InkFlow.Infrastructure;

public class ReportConfig:IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(x => x.ReportId);
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.Reason).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ReporterUserId).IsRequired();
        builder.Property(x => x.ArticleId).IsRequired();
        builder.Property(x => x.CommentId).IsRequired();
    }
}
