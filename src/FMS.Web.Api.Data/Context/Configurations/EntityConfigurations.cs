using FMS.Domain.entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMS.Web.Api.Data.Context.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Login).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TwoFactorSecret).HasMaxLength(200);

        builder.HasIndex(x => x.Login).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RefreshTokenHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.RevokedReason).HasMaxLength(200);

        builder.HasOne(x => x.UserEntity)
               .WithMany(x => x.Sessions)
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
    }
}

public class UserLoginAttemptConfiguration : IEntityTypeConfiguration<UserLoginAttempt>
{
    public void Configure(EntityTypeBuilder<UserLoginAttempt> builder)
    {
        builder.ToTable("UserLoginAttempts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Login).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasIndex(x => x.Login);
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(100);
        builder.Property(x => x.URL).HasMaxLength(500);
        builder.Property(x => x.Page).HasMaxLength(200);

        builder.HasOne(x => x.MenuItemSuperiorEntity)
               .WithMany(x => x.Children)
               .HasForeignKey(x => x.MenuItemSuperiorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class UserMenuPermissionConfiguration : IEntityTypeConfiguration<UserMenuPermission>
{
    public void Configure(EntityTypeBuilder<UserMenuPermission> builder)
    {
        builder.ToTable("UserMenuPermissions");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.UserEntity)
               .WithMany(x => x.Permissions)
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MenuItemEntity)
               .WithMany(x => x.Permissions)
               .HasForeignKey(x => x.MenuItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.MenuItemId }).IsUnique();
    }
}
