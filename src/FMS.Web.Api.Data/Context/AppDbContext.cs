using System.Linq.Expressions;
using FMS.Domain.entities;
using Microsoft.EntityFrameworkCore;

namespace FMS.Web.Api.Data.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>               Users              => Set<User>();
    public DbSet<UserSession>        UserSessions       => Set<UserSession>();
    public DbSet<UserLoginAttempt>   UserLoginAttempts  => Set<UserLoginAttempt>();
    public DbSet<MenuItem>           MenuItems          => Set<MenuItem>();
    public DbSet<UserMenuPermission> UserMenuPermissions => Set<UserMenuPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Exclusão lógica: registros com SystemIsDeleted não aparecem nas consultas.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(EntityControl).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var notDeleted = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(parameter, nameof(EntityControl.SystemIsDeleted)),
                    Expression.Constant(false)),
                parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(notDeleted);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntityControl>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
