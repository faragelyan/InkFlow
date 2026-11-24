namespace InkFlow.Application.Features.Topics;

public static class TopicErrors
{
    public const string TopicNotFound = "Topic not found.";
    public const string DuplicateName = "A topic with this name already exists.";
    public const string DuplicateSlug = "A topic with this slug already exists.";
    public const string TopicInUse = "Topic cannot be deleted because articles are assigned to it.";
    public const string SaveFailed = "Unable to save topic.";
    public const string UpdateFailed = "Unable to update topic.";
    public const string DeleteFailed = "Unable to delete topic.";
}
