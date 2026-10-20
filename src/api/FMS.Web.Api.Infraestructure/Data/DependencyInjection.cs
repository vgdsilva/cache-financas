using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FMS.Web.Api.Infraestructure.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Registra o AppDbContext. Compartilhado pela API e pelo projeto de migrations;
    /// este ultimo informa o assembly onde as migrations sao geradas e aplicadas.
    /// </summary>
    public static IServiceCollection AddAppDbContext(
        this IServiceCollection services,
        string connectionString,
        string? migrationsAssembly = null)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                if (migrationsAssembly is not null)
                    npgsql.MigrationsAssembly(migrationsAssembly);
            }));

        return services;
    }
}
