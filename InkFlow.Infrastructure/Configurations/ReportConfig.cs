using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;

namespace InkFlow.Infrastructure;

public class ReportConfig : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(x => x.ReportId);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ReporterUserId).IsRequired();
        builder.Property(x => x.ArticleId);
        builder.Property(x => x.CommentId);

        // Filtered index for pending moderation queue (oldest reports first)
        builder.HasIndex(x => x.CreatedAt)
            .HasFilter("[Status] = 1");

        // Navigation properties
        builder.HasOne(x => x.Reporter)
            .WithMany(u => u.Reports)
            .HasForeignKey(x => x.ReporterUserId);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.Reports)
            .HasForeignKey(x => x.ArticleId);

        builder.HasOne(x => x.Comment)
            .WithMany(c => c.Reports)
            .HasForeignKey(x => x.CommentId);
    }
}
