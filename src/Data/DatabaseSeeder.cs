using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Data;

/// <summary>Carga inicial idempotente: sincroniza os catálogos (menus e configurações) e cria o administrador inicial.</summary>
public sealed class DatabaseSeeder(AppDbContext db, ICommandHandler<EnsureInitialAdminCommand, bool> ensureInitialAdmin, IConfiguration configuration, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedMenusAsync(cancellationToken);
        await SeedConfigurationsAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
    }

    public async Task SeedConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: uma configuração excluída de propósito não deve ser recriada.
        var existingKeys = await db.Configurations
            .IgnoreQueryFilters()
            .Where(c => c.UserId == null)
            .Select(c => c.Key)
            .ToListAsync(cancellationToken);

        foreach (var definition in ConfigurationKeys.All.Where(d => !existingKeys.Contains(d.Key)))
        {
            db.Configurations.Add(new Configuration { Key = definition.Key, Value = definition.DefaultValue });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedMenusAsync(CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: um menu excluído de propósito não deve ser recriado.
        var all = await db.MenuItems.IgnoreQueryFilters().ToListAsync(cancellationToken);

        var byCode = all
            .GroupBy(m => m.Code)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.SystemDeleted == Guid.Empty).First());

        foreach (var definition in Menus.All)
        {
            if (byCode.TryGetValue(definition.Code, out var existing))
            {
                if (existing.SystemDeleted == Guid.Empty)
                {
                    // O catálogo é a fonte da verdade da estrutura (texto, ícone, URL, ordem e pai).
                    // IsActive não é tocado aqui: desativar um menu à mão continua valendo.
                    existing.Description = definition.Description;
                    existing.Icon = definition.Icon;
                    existing.Url = definition.Url;
                    existing.SortOrder = definition.SortOrder;
                    existing.ParentId = definition.ParentCode is null ? null : byCode[definition.ParentCode].Id;
                }

                continue;
            }

            var menu = new MenuItem
            {
                Code = definition.Code,
                Description = definition.Description,
                Icon = definition.Icon,
                Url = definition.Url,
                SortOrder = definition.SortOrder,
                ParentId = definition.ParentCode is null ? null : byCode[definition.ParentCode].Id,
            };

            db.MenuItems.Add(menu);
            byCode[definition.Code] = menu;
        }

        // Menu que saiu do catálogo é desativado (não apagado: pode haver permissões apontando para ele).
        var catalogCodes = Menus.All.Select(d => d.Code).ToHashSet();

        foreach (var obsolete in all.Where(m => m.SystemDeleted == Guid.Empty && m.IsActive && !catalogCodes.Contains(m.Code)))
        {
            obsolete.IsActive = false;
            logger.LogInformation("Menu '{Code}' não está mais no catálogo e foi desativado.", obsolete.Code);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var userName = configuration["Seed:AdminUserName"];
        var password = configuration["Seed:AdminPassword"];
        var email = configuration["Seed:AdminEmail"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Seed:AdminUserName/Seed:AdminPassword/Seed:AdminEmail não configurados: administrador inicial não será criado.");
            return;
        }

        if (await ensureInitialAdmin.HandleAsync(new EnsureInitialAdminCommand(userName, password, email), cancellationToken))
        {
            logger.LogInformation("Administrador inicial '{UserName}' criado.", UserNames.Normalize(userName));
        }
    }
}
