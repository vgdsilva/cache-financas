using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Data;

/// <summary>
/// Na subida da aplicação: aplica as migrations pendentes e, em seguida, roda o seeder
/// (catálogos e administrador inicial). Desligue tudo com <c>Database:InitializeOnStartup=false</c>
/// (usado pelos testes sem banco); neste caso o schema deve ser aplicado por fora (<c>dotnet ef database update</c>).
/// </summary>
public sealed class DatabaseInitializer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Database:InitializeOnStartup", true))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count > 0)
        {
            logger.LogInformation("Aplicando {Count} migration(s) pendente(s): {Migrations}", pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
        }

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
