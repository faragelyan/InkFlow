namespace InkFlow.Application.Features.Authors;

public static class AuthorErrors
{
    public const string NotFound = "Author not found.";
    public const string ProfileUpdateFailed = "Unable to update author profile.";
    public const string CannotFollowSelf = "You cannot follow yourself.";
    public const string FollowUpdateFailed = "Unable to update follow status.";
}