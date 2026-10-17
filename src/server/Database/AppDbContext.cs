using Microsoft.EntityFrameworkCore;

namespace CacheFinancas.Server.Database;

/// <summary>
/// Contexto unico, sem DbSet por entidade: cada modulo traz seu IEntityTypeConfiguration
/// (descoberto abaixo) e os servicos usam Set de T.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
