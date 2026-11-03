using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.Framework.Extensions;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Data;

public class DatabaseInitializerTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    /// <summary>Banco novo e vazio (só o servidor existe), como na primeira subida do app.</summary>
    private ServiceProvider BuildApp(string database, IDictionary<string, string?> extra)
    {
        var connection = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = database }.ConnectionString;

        var settings = new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connection };
        foreach (var (key, value) in extra)
        {
            settings[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFrameworkDatabase(configuration);
        services.AddHandlers(typeof(Program).Assembly);

        return services.BuildServiceProvider();
    }

    private static async Task RunInitializerAsync(ServiceProvider provider)
    {
        var initializer = provider.GetServices<IHostedService>().OfType<DatabaseInitializer>().Single();

        await initializer.StartAsync(CancellationToken.None);
    }

    [DockerFact]
    public async Task EmptyDatabase_IsMigrated_AndSeeded_WithDevelopmentAdmin()
    {
        await using var provider = BuildApp("init_" + Guid.NewGuid().ToString("N"), new Dictionary<string, string?>
        {
            ["Auth:MinPasswordLength"] = "3",
            ["Seed:AdminUserName"] = "admin",
            ["Seed:AdminPassword"] = "123",
            ["Seed:AdminEmail"] = "admin@cachefinancas.local",
        });

        await RunInitializerAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.True(await db.MenuItems.AnyAsync(m => m.Code == Menus.Transactions));
        Assert.True(await db.Configurations.AnyAsync(c => c.Key == ConfigurationKeys.AiModel));

        var authenticate = scope.ServiceProvider.GetRequiredService<ICommandHandler<AuthenticateUserCommand, AuthenticatedUser?>>();
        var admin = await authenticate.HandleAsync(new AuthenticateUserCommand("admin", "123"));
        Assert.NotNull(admin);
        Assert.True(await db.Users.AnyAsync(u => u.UserName == "admin" && u.IsAdmin));
    }

    [DockerFact]
    public async Task RunningTwice_IsIdempotent()
    {
        await using var provider = BuildApp("init_" + Guid.NewGuid().ToString("N"), new Dictionary<string, string?>
        {
            ["Auth:MinPasswordLength"] = "3",
            ["Seed:AdminUserName"] = "admin",
            ["Seed:AdminPassword"] = "123",
            ["Seed:AdminEmail"] = "admin@cachefinancas.local",
        });

        await RunInitializerAsync(provider);
        await RunInitializerAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [DockerFact]
    public async Task ShortPassword_IsRejected_WhenMinLengthIsDefault()
    {
        await using var provider = BuildApp("init_" + Guid.NewGuid().ToString("N"), new Dictionary<string, string?>
        {
            ["Seed:AdminUserName"] = "admin",
            ["Seed:AdminPassword"] = "123",
            ["Seed:AdminEmail"] = "admin@cachefinancas.local",
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => RunInitializerAsync(provider));
    }

    [Fact]
    public async Task Disabled_DoesNothing_EvenWithoutDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:InitializeOnStartup"] = "false" })
            .Build();
        var initializer = new DatabaseInitializer(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseInitializer>.Instance);

        await initializer.StartAsync(CancellationToken.None);
    }
}
