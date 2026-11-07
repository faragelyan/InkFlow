using Microsoft.AspNetCore.Identity;

namespace InkFlow.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<Follow> Followings { get; set; } = new List<Follow>();

    public ICollection<ArticleBookmark> ArticleBookmarks { get; set; } = new List<ArticleBookmark>();
    public ICollection<ArticleLike> ArticleLikes { get; set; } = new List<ArticleLike>();
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
