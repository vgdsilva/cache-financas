using FMS.Web.Api.Data.Context;
using FMS.Web.Api.Data.Repositories;
using FMS.Web.Api.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FMS.Web.Api.Data.Extensions;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserLoginAttemptRepository, UserLoginAttemptRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IUserMenuPermissionRepository, UserMenuPermissionRepository>();

        return services;
    }

    public static IServiceCollection AddDatabaseSeed(this IServiceCollection services, SeedOptions options)
    {
        services.AddSingleton(options);
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes e a carga inicial (se registrada). Falhas são registradas em log e não derrubam a aplicação.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Migrations aplicadas com sucesso.");

            var seeder = scope.ServiceProvider.GetService<DatabaseSeeder>();
            if (seeder is not null)
                await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao aplicar migrations/seed. A aplicação continuará iniciando.");
        }
    }
}
