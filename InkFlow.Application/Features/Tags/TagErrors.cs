namespace InkFlow.Application.Features.Tags;

public static class TagErrors
{
    public const string TagNotFound = "Tag not found.";
    public const string DuplicateName = "A tag with this name already exists.";
    public const string DuplicateSlug = "A tag with this slug already exists.";
    public const string TagInUse = "Tag cannot be deleted because articles are assigned to it.";
    public const string SaveFailed = "Unable to save tag.";
    public const string UpdateFailed = "Unable to update tag.";
    public const string DeleteFailed = "Unable to delete tag.";
}
