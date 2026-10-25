using FMS.Domain.entities;
using FMS.Web.Api.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FMS.Web.Api.Data.Seed;

/// <summary>Carga inicial idempotente: menus do sistema e usuário administrador.</summary>
public class DatabaseSeeder(AppDbContext context, SeedOptions options, ILogger<DatabaseSeeder> logger)
{
    private sealed record MenuSeed(string Code, string Description, string? ParentCode, string? Icon, string? Url, int Ordem);

    private static readonly MenuSeed[] Menus =
    [
        new("HOME",            "Home",          null,     "house",    "/",         1),
        new("CONFIG",          "Configurações", null,     "gear",     null,        90),
        new("CONFIG.USUARIOS", "Usuários",      "CONFIG", "people",   "/usuarios", 1),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedMenusAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
    }

    private async Task SeedMenusAsync(CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: um menu removido logicamente não deve ser recriado.
        var existing = await context.MenuItems.IgnoreQueryFilters().ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var seed in Menus)
        {
            if (existing.ContainsKey(seed.Code))
                continue;

            var menu = new MenuItem
            {
                Id = Guid.NewGuid(),
                Code = seed.Code,
                Description = seed.Description,
                Icon = seed.Icon,
                URL = seed.Url,
                Ordem = seed.Ordem,
                MenuItemSuperiorId = seed.ParentCode is null ? null : existing[seed.ParentCode].Id
            };

            context.MenuItems.Add(menu);
            existing[seed.Code] = menu;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        await SeedUserAsync(options.Admin, "Admin", isAdmin: true, cancellationToken);
        await SeedUserAsync(options.User, "Seed:User", isAdmin: false, cancellationToken);
    }

    private async Task SeedUserAsync(SeedUser seed, string name, bool isAdmin, CancellationToken cancellationToken)
    {
        var login = seed.Login.Trim();

        if (await context.Users.IgnoreQueryFilters().AnyAsync(x => x.Login.ToLower() == login.ToLower(), cancellationToken))
            return;

        if (string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogInformation("Usuário '{Login}' não criado: senha não configurada ({Name}).", login, name);
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = seed.Name,
            Login = login,
            Email = seed.Email.Trim(),
            IsAdmin = isAdmin,
            MustChangePassword = isAdmin
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, seed.Password);
        context.Users.Add(user);

        // Usuário comum começa apenas com acesso de visualização à Home; admin enxerga tudo sem linhas de permissão.
        if (!isAdmin)
        {
            var home = await context.MenuItems.FirstOrDefaultAsync(x => x.Code == "HOME", cancellationToken);
            if (home is not null)
            {
                context.UserMenuPermissions.Add(new UserMenuPermission
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    MenuItemId = home.Id,
                    CanView = true
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuário '{Login}' criado.", login);
    }
}
