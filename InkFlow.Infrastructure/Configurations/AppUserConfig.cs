using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InkFlow.Domain;
namespace InkFlow.Infrastructure;

public class AppUserConfig:IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Bio).HasMaxLength(500);
        builder.Property(x => x.ProfileImageUrl).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        //navigation prop         
        builder.HasMany(x => x.Articles).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);  
        builder.HasMany(x => x.Comments).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId); 
        builder.HasMany(x => x.ArticleBookmarks).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);
        builder.HasMany(x => x.Followings).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);     
        builder.HasMany(x => x.Followers).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);
        builder.HasMany(x => x.Notifications).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId); 
        builder.HasMany(x => x.ArticleLikes).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);     
        builder.HasMany(x => x.CommentLikes).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);
        builder.HasMany(x => x.Reports).WithOne(x => x.AppUser).HasForeignKey(x => x.UserId);    

    }
}
