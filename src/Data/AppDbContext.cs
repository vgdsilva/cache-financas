using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data.Types;

namespace VGS.FinanceIA.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Configuration> Configurations => Set<Configuration>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<UserMenuPermission> UserMenuPermissions => Set<UserMenuPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entidades, colunas, índices e FKs são declarados por atributos nas classes (Data/Types).
        // Aqui ficam só as regras transversais de tudo que herda de EntityControl.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(EntityControl).IsAssignableFrom(t.ClrType)))
        {
            // Exclusão lógica: só registros ativos (system_deleted = Guid.Empty).
            var e = Expression.Parameter(entityType.ClrType, "e");
            var isActive = Expression.Equal(
                Expression.Property(e, nameof(EntityControl.SystemDeleted)),
                Expression.Constant((Guid?)Guid.Empty, typeof(Guid?)));
            entityType.SetQueryFilter(Expression.Lambda(isActive, e));

            // NULLS NOT DISTINCT: UserId nulo (config global) também não pode duplicar.
            foreach (var index in entityType.GetIndexes().Where(i => i.IsUnique))
            {
                index.SetAreNullsDistinct(false);
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyEntityControlRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyEntityControlRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Infraestrutura de persistência (não é regra de negócio): carimbos de data em UTC e
    /// conversão de Remove em exclusão lógica (system_deleted = id do próprio registro).
    /// </summary>
    internal void ApplyEntityControlRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<EntityControl>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateAt = now;
                    entry.Entity.UpdateAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdateAt = now;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.SystemDeleted = entry.Entity.Id;
                    entry.Entity.SystemDeletedAt = now;
                    entry.Entity.UpdateAt = now;
                    break;
            }
        }
    }
}
