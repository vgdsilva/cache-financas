using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Permissions;

public sealed record RevokeMenuAccessCommand(Guid UserId, string MenuCode) : ICommand;

public sealed class RevokeMenuAccessHandler(AppDbContext db, IMemoryCache cache) : ICommandHandler<RevokeMenuAccessCommand>
{
    public async Task HandleAsync(RevokeMenuAccessCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.MenuCode);

        var permissions = await db.UserMenuPermissions
            .Where(p => p.UserId == command.UserId && p.MenuItem!.Code == command.MenuCode)
            .ToListAsync(cancellationToken);

        if (permissions.Count > 0)
        {
            db.UserMenuPermissions.RemoveRange(permissions);
            await db.SaveChangesAsync(cancellationToken);
        }

        cache.Remove(PermissionCache.Key(command.UserId));
    }
}
