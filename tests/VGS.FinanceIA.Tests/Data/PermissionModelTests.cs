using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;

namespace VGS.FinanceIA.Tests.Data;

public class PermissionModelTests
{
    private static IModel DesignModel()
    {
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);

        return db.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void MenuItem_CodeIsUniqueAmongActiveRows()
    {
        var index = DesignModel().FindEntityType(typeof(MenuItem))!.GetIndexes().Single(i => i.IsUnique);

        Assert.Equal(["Code", "SystemDeleted"], index.Properties.Select(p => p.Name));
    }

    [Fact]
    public void UserMenuPermission_PairIsUniqueAmongActiveRows()
    {
        var index = DesignModel().FindEntityType(typeof(UserMenuPermission))!.GetIndexes().Single(i => i.IsUnique);

        Assert.Equal(["UserId", "MenuItemId", "SystemDeleted"], index.Properties.Select(p => p.Name));
    }

    [Fact]
    public void UserMenuPermission_HasForeignKeysToUserAndMenu()
    {
        var foreignKeys = DesignModel().FindEntityType(typeof(UserMenuPermission))!.GetForeignKeys()
            .Select(fk => fk.PrincipalEntityType.ClrType)
            .ToList();

        Assert.Contains(typeof(User), foreignKeys);
        Assert.Contains(typeof(MenuItem), foreignKeys);
    }

    [Fact]
    public void AllEntityControlTypes_HaveSoftDeleteFilter()
    {
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);

        Assert.Contains("system_deleted", db.MenuItems.ToQueryString());
        Assert.Contains("system_deleted", db.UserMenuPermissions.ToQueryString());
    }
}
