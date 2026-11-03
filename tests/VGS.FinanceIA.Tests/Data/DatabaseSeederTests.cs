using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Data;

public class DatabaseSeederTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [DockerFact]
    public async Task SeedMenus_IsIdempotent()
    {
        await using var db = postgres.CreateDbContext();

        await TestHandlers.Seeder(db).SeedMenusAsync();
        await TestHandlers.Seeder(db).SeedMenusAsync();

        var codes = await db.MenuItems.Select(m => m.Code).ToListAsync();
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.All(Menus.All, definition => Assert.Contains(definition.Code, codes));
    }

    [DockerFact]
    public async Task SeedMenus_LinksChildToParent()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();

        var users = await db.MenuItems.Include(m => m.Parent).SingleAsync(m => m.Code == Menus.Users);

        Assert.Equal(Menus.Settings, users.Parent?.Code);
    }

    [DockerFact]
    public async Task SeedMenus_DoesNotRecreateDeletedMenu()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();

        db.MenuItems.Remove(await db.MenuItems.SingleAsync(m => m.Code == Menus.Categories));
        await db.SaveChangesAsync();
        await TestHandlers.Seeder(db).SeedMenusAsync();

        Assert.False(await db.MenuItems.AnyAsync(m => m.Code == Menus.Categories));
    }

    [DockerFact]
    public async Task SeedConfigurations_CreatesDefaults_AndNeverOverwritesEditedValue()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedConfigurationsAsync();

        var aiModel = await db.Configurations.SingleAsync(c => c.Key == ConfigurationKeys.AiModel && c.UserId == null);
        Assert.Equal(ConfigurationKeys.All.Single(d => d.Key == ConfigurationKeys.AiModel).DefaultValue, aiModel.Value);

        aiModel.Value = "outro-modelo";
        await db.SaveChangesAsync();
        await TestHandlers.Seeder(db).SeedConfigurationsAsync();

        await using var fresh = postgres.CreateDbContext();
        Assert.Equal("outro-modelo", (await fresh.Configurations.SingleAsync(c => c.Key == ConfigurationKeys.AiModel && c.UserId == null)).Value);
    }

    [DockerFact]
    public async Task SeedAsync_WithoutAdminSettings_DoesNotCreateUser()
    {
        await using var db = postgres.CreateDbContext();
        var before = await db.Users.CountAsync();

        await TestHandlers.Seeder(db).SeedAsync();

        Assert.Equal(before, await db.Users.CountAsync());
    }

    [DockerFact]
    public async Task SeedAsync_WithAdminSettings_CreatesAdminOnlyOnce()
    {
        await using var db = postgres.CreateDbContext();
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:AdminUserName"] = TestHandlers.UniqueUserName(),
            ["Seed:AdminPassword"] = TestHandlers.ValidPassword,
        }).Build();

        await TestHandlers.Seeder(db, settings).SeedAsync();
        await TestHandlers.Seeder(db, settings).SeedAsync();

        Assert.True(await db.Users.AnyAsync(u => u.IsAdmin));
    }
}
