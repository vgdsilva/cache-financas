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
        await SeedAdminAsync(cancellationToken);
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

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var login = options.Login.Trim();

        if (await context.Users.IgnoreQueryFilters().AnyAsync(x => x.Login.ToLower() == login.ToLower(), cancellationToken))
            return;

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Usuário admin não criado: {Section}:Password não configurada.", SeedOptions.Section);
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = options.Name,
            Login = login,
            Email = options.Email.Trim(),
            IsAdmin = true,
            MustChangePassword = true
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, options.Password);

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuário admin '{Login}' criado.", login);
    }
}
