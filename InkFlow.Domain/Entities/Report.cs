namespace InkFlow.Domain;

public class Report
{
    public Guid ReportId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Reporter User
    public Guid ReporterUserId { get; set; }
    public AppUser Reporter { get; set; } = null!;

    // Optional Targets
    public Guid? ArticleId { get; set; }
    public Article? Article { get; set; }

    public Guid? CommentId { get; set; }
    public Comment? Comment { get; set; }
}
