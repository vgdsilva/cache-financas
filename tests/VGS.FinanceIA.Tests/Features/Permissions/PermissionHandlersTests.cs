using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Features.Permissions;

public class PermissionHandlersTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [DockerFact]
    public async Task Admin_SeesAllActiveMenus()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();
        var adminId = await TestHandlers.CreateUserAsync(db, isAdmin: true);

        var allowed = await new TestHandlers.Permissions(db).GetAllowed.HandleAsync(new GetAllowedMenusQuery(adminId));

        var active = await db.MenuItems.Where(m => m.IsActive).Select(m => m.Code).ToListAsync();
        Assert.Equal(active.Order(), allowed.Select(m => m.Code).Order());
    }

    [DockerFact]
    public async Task RegularUser_GrantAndRevoke_TakeEffectImmediately()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();
        var userId = await TestHandlers.CreateUserAsync(db, isAdmin: false);
        var permissions = new TestHandlers.Permissions(db);

        Assert.False(await permissions.HasAsync(userId, Menus.Transactions));

        await permissions.Grant.HandleAsync(new GrantMenuAccessCommand(userId, Menus.Transactions));
        Assert.True(await permissions.HasAsync(userId, Menus.Transactions));
        Assert.False(await permissions.HasAsync(userId, Menus.Users));

        await permissions.Revoke.HandleAsync(new RevokeMenuAccessCommand(userId, Menus.Transactions));
        Assert.False(await permissions.HasAsync(userId, Menus.Transactions));
    }

    [DockerFact]
    public async Task Grant_AfterRevoke_Works_AndIsIdempotent()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();
        var userId = await TestHandlers.CreateUserAsync(db, isAdmin: false);
        var permissions = new TestHandlers.Permissions(db);

        await permissions.Grant.HandleAsync(new GrantMenuAccessCommand(userId, Menus.Chat));
        await permissions.Grant.HandleAsync(new GrantMenuAccessCommand(userId, Menus.Chat));
        await permissions.Revoke.HandleAsync(new RevokeMenuAccessCommand(userId, Menus.Chat));
        await permissions.Grant.HandleAsync(new GrantMenuAccessCommand(userId, Menus.Chat));

        Assert.True(await permissions.HasAsync(userId, Menus.Chat));
        Assert.Equal(1, await db.UserMenuPermissions.CountAsync(p => p.UserId == userId));
    }

    [DockerFact]
    public async Task DeletedUser_HasNoAccess()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();
        var adminId = await TestHandlers.CreateUserAsync(db, isAdmin: true);
        Assert.True(await new TestHandlers.Permissions(db).HasAsync(adminId, Menus.Chat));

        db.Users.Remove(await db.Users.SingleAsync(u => u.Id == adminId));
        await db.SaveChangesAsync();

        // Cache novo para refletir o banco (o TTL do cache é só rede de segurança).
        Assert.False(await new TestHandlers.Permissions(db).HasAsync(adminId, Menus.Chat));
    }

    [DockerFact]
    public async Task Grant_UnknownMenu_Throws()
    {
        await using var db = postgres.CreateDbContext();
        await TestHandlers.Seeder(db).SeedMenusAsync();
        var userId = await TestHandlers.CreateUserAsync(db, isAdmin: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TestHandlers.Permissions(db).Grant.HandleAsync(new GrantMenuAccessCommand(userId, "NOPE")));
    }
}
