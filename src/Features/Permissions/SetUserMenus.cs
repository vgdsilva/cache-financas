using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Permissions;

/// <summary>
/// Define exatamente quais menus o usuário acessa: concede os que faltam e revoga os que sobram.
/// Só aceita menus ativos que são páginas (agrupadores não são atribuíveis).
/// </summary>
public sealed record SetUserMenusCommand(Guid UserId, IReadOnlyCollection<string> MenuCodes) : ICommand;

public sealed class SetUserMenusHandler(AppDbContext db, IMemoryCache cache) : ICommandHandler<SetUserMenusCommand>
{
    public async Task HandleAsync(SetUserMenusCommand command, CancellationToken cancellationToken = default)
    {
        var codes = (command.MenuCodes ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        var menus = await db.MenuItems
            .Where(m => m.IsActive && m.Url != null && codes.Contains(m.Code))
            .ToListAsync(cancellationToken);

        var unknown = codes.Except(menus.Select(m => m.Code)).ToList();

        if (unknown.Count > 0)
        {
            throw new BusinessRuleException($"Menu inválido: {string.Join(", ", unknown)}.");
        }

        var wanted = menus.Select(m => m.Id).ToHashSet();
        var current = await db.UserMenuPermissions.Where(p => p.UserId == command.UserId).ToListAsync(cancellationToken);

        db.UserMenuPermissions.RemoveRange(current.Where(p => !wanted.Contains(p.MenuItemId)));

        var alreadyGranted = current.Select(p => p.MenuItemId).ToHashSet();

        foreach (var menuId in wanted.Where(id => !alreadyGranted.Contains(id)))
        {
            db.UserMenuPermissions.Add(new UserMenuPermission { UserId = command.UserId, MenuItemId = menuId });
        }

        await db.SaveChangesAsync(cancellationToken);

        cache.Remove(PermissionCache.Key(command.UserId));
    }
}
