using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Data;

public class AppDbContextTests
{
    private static AppDbContext NewOfflineDb() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);

    [Fact]
    public void Model_IsInSyncWithMigrations()
    {
        using var db = NewOfflineDb();

        Assert.False(
            db.Database.HasPendingModelChanges(),
            "O modelo mudou sem migration. Rode: dotnet dotnet-ef migrations add <Nome> --project src");
    }

    [Fact]
    public void UniqueIndexes_IncludeSystemDeleted()
    {
        using var db = NewOfflineDb();

        var model = db.GetService<IDesignTimeModel>().Model;

        var userIndex = model.FindEntityType(typeof(User))!.GetIndexes().Single();
        Assert.True(userIndex.IsUnique);
        Assert.Equal(["UserName", "SystemDeleted"], userIndex.Properties.Select(p => p.Name));

        var configIndex = model.FindEntityType(typeof(Configuration))!.GetIndexes().Single(i => i.IsUnique);
        Assert.Equal(["Key", "UserId", "SystemDeleted"], configIndex.Properties.Select(p => p.Name));
        Assert.False(configIndex.GetAreNullsDistinct());
    }

    [Fact]
    public void Add_StampsUtcDates()
    {
        using var db = NewOfflineDb();
        var user = new User { UserName = "vinicius", PasswordHash = "hash" };

        db.Add(user);
        db.ApplyEntityControlRules();

        Assert.Equal(DateTimeKind.Utc, user.CreateAt.Kind);
        Assert.Equal(DateTimeKind.Utc, user.UpdateAt.Kind);
    }

    [Fact]
    public void Remove_BecomesSoftDeleteWithOwnId()
    {
        using var db = NewOfflineDb();
        var user = new User { UserName = "vinicius", PasswordHash = "hash" };
        db.Attach(user);
        Assert.Equal(Guid.Empty, user.SystemDeleted);

        db.Remove(user);
        db.ApplyEntityControlRules();

        Assert.Equal(EntityState.Modified, db.Entry(user).State);
        Assert.Equal(user.Id, user.SystemDeleted);
        Assert.NotNull(user.SystemDeletedAt);
        Assert.Equal(DateTimeKind.Utc, user.SystemDeletedAt!.Value.Kind);
    }
}

public class AppDbContextPostgresTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [DockerFact]
    public async Task DuplicateActiveUserName_IsRejected()
    {
        await using var db = postgres.CreateDbContext();
        db.Users.Add(new User { UserName = "dup", PasswordHash = "h" });
        await db.SaveChangesAsync();

        await using var db2 = postgres.CreateDbContext();
        db2.Users.Add(new User { UserName = "dup", PasswordHash = "h" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
    }

    [DockerFact]
    public async Task DeletedUserName_CanBeReused_AndRowIsKept()
    {
        await using var db = postgres.CreateDbContext();
        var first = new User { UserName = "reuse", PasswordHash = "h" };
        db.Users.Add(first);
        await db.SaveChangesAsync();

        db.Users.Remove(first);
        await db.SaveChangesAsync();

        db.Users.Add(new User { UserName = "reuse", PasswordHash = "h2" });
        await db.SaveChangesAsync();

        var all = await db.Users.IgnoreQueryFilters().Where(u => u.UserName == "reuse").ToListAsync();
        Assert.Equal(2, all.Count);
        Assert.Single(await db.Users.Where(u => u.UserName == "reuse").ToListAsync());
        Assert.Equal(first.Id, all.Single(u => u.Id == first.Id).SystemDeleted);
    }

    [DockerFact]
    public async Task DuplicateGlobalConfigurationKey_IsRejected()
    {
        await using var db = postgres.CreateDbContext();
        db.Configurations.Add(new Configuration { Key = "Ai:Model", Value = "a" });
        await db.SaveChangesAsync();

        await using var db2 = postgres.CreateDbContext();
        db2.Configurations.Add(new Configuration { Key = "Ai:Model", Value = "b" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
    }
}

public class SoftDeleteFilterTests
{
    [Fact]
    public void Queries_OnlyReturnActiveRows()
    {
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);

        var userSql = db.Users.ToQueryString();
        var configSql = db.Configurations.ToQueryString();

        Assert.Contains("system_deleted", userSql);
        Assert.Contains("system_deleted", configSql);
        Assert.DoesNotContain("WHERE", db.Users.IgnoreQueryFilters().ToQueryString());
    }
}
