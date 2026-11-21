using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InkFlow.Domain;

namespace InkFlow.Infrastructure.Persistence.Context;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<ArticleTopic> ArticleTopics => Set<ArticleTopic>();
    public DbSet<ArticleBookmark> ArticleBookmarks => Set<ArticleBookmark>();
    public DbSet<ArticleLike> ArticleLikes => Set<ArticleLike>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must be called first to configure Identity tables
        base.OnModelCreating(builder);

        // Apply all entity configurations from the current assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.HasChanges())
            ApplyAuditAndSoftDeleteRules();

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        if (ChangeTracker.HasChanges())
            ApplyAuditAndSoftDeleteRules();

        return base.SaveChanges();
    }

    private void ApplyAuditAndSoftDeleteRules()
    {
        var utcNow = DateTime.UtcNow;

        // Only process our own domain entities, skip Identity framework internals
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                {
                    var createdAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                    if (createdAt != null && createdAt.CurrentValue is DateTime dt && dt == default)
                        createdAt.CurrentValue = utcNow;
                    break;
                }

                case EntityState.Modified:
                {
                    var updatedAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                    if (updatedAt != null)
                        updatedAt.CurrentValue = utcNow;
                    break;
                }

                case EntityState.Deleted:
                {
                    // Only AppUser and Comment support soft delete
                    // All other entities are hard-deleted
                    var isDeleted = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
                    if (isDeleted != null)
                    {
                        entry.State = EntityState.Modified;
                        isDeleted.CurrentValue = true;

                        var deletedAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
                        if (deletedAt != null)
                            deletedAt.CurrentValue = utcNow;
                    }
                    break;
                }
            }
        }
    }
}

