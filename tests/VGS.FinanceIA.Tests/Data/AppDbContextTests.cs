using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Domain;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Data;

public class AppDbContextTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public void Model_IsInSyncWithMigrations()
    {
        // Não conecta ao banco: só compara o modelo atual com o snapshot das migrations.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;
        using var db = new AppDbContext(options);

        Assert.False(
            db.Database.HasPendingModelChanges(),
            "O modelo mudou sem migration. Rode: dotnet dotnet-ef migrations add <Nome> --project src");
    }

    [DockerFact]
    public async Task Migrations_SeedDefaultAiModel()
    {
        await using var db = postgres.CreateDbContext();

        var setting = await db.Settings.SingleAsync(s => s.Key == SettingKeys.AiModel);

        Assert.Equal(SettingKeys.DefaultAiModel, setting.Value);
    }

    [DockerFact]
    public async Task Task_RoundTrips()
    {
        var task = new TaskItem
        {
            Title = "Pagar internet",
            DueDate = new DateOnly(2026, 11, 4),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using (var write = postgres.CreateDbContext())
        {
            write.Tasks.Add(task);
            await write.SaveChangesAsync();
        }

        await using var read = postgres.CreateDbContext();
        var loaded = await read.Tasks.SingleAsync(t => t.Id == task.Id);

        Assert.Equal("Pagar internet", loaded.Title);
        Assert.Equal(new DateOnly(2026, 11, 4), loaded.DueDate);
        Assert.False(loaded.IsDone);
    }
}
