using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Permissions;

public sealed record GrantMenuAccessCommand(Guid UserId, string MenuCode) : ICommand;

public sealed class GrantMenuAccessHandler(AppDbContext db, IMemoryCache cache) : ICommandHandler<GrantMenuAccessCommand>
{
    public async Task HandleAsync(GrantMenuAccessCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.MenuCode);

        var menu = await db.MenuItems.SingleOrDefaultAsync(m => m.Code == command.MenuCode && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Menu '{command.MenuCode}' não existe ou está inativo.");

        var alreadyGranted = await db.UserMenuPermissions
            .AnyAsync(p => p.UserId == command.UserId && p.MenuItemId == menu.Id, cancellationToken);

        if (!alreadyGranted)
        {
            db.UserMenuPermissions.Add(new UserMenuPermission { UserId = command.UserId, MenuItemId = menu.Id });
            await db.SaveChangesAsync(cancellationToken);
        }

        cache.Remove(PermissionCache.Key(command.UserId));
    }
}
