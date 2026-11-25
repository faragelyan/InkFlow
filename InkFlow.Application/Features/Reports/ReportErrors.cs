namespace InkFlow.Application.Features.Reports;

public static class ReportErrors
{
    public const string NotFound = "Report not found.";
    public const string ArticleNotFound = "Published article not found.";
    public const string CommentNotFound = "Comment not found.";
    public const string InvalidTarget = "A report must target exactly one article or comment.";
    public const string ActiveUserNotFound = "Active user not found.";
    public const string Duplicate = "You have already reported this target.";
    public const string SaveFailed = "Unable to save report.";
    public const string UpdateFailed = "Unable to update report.";
}
