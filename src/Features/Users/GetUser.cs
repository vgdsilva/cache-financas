using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary><paramref name="MenuCodes"/> são os menus concedidos diretamente (ignorados para administrador).</summary>
public sealed record UserDetail(Guid Id, string UserName, bool IsAdmin, IReadOnlyList<string> MenuCodes);

/// <summary>Um usuário ativo, ou nulo se não existir (ou tiver sido excluído).</summary>
public sealed record GetUserQuery(Guid UserId) : IQuery<UserDetail?>;

public sealed class GetUserHandler(AppDbContext db) : IQueryHandler<GetUserQuery, UserDetail?>
{
    public async Task<UserDetail?> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new { u.Id, u.UserName, u.IsAdmin })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var codes = await db.UserMenuPermissions
            .Where(p => p.UserId == user.Id)
            .Select(p => p.MenuItem!.Code)
            .ToListAsync(cancellationToken);

        return new UserDetail(user.Id, user.UserName, user.IsAdmin, codes);
    }
}
