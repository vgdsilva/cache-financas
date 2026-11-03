using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Permissions;

/// <summary>Menu liberado ao usuário. <paramref name="ParentCode"/> é o menu pai (agrupador), quando houver.</summary>
public sealed record MenuEntry(string Code, string Description, string? Icon, string? Url, int SortOrder, string? ParentCode = null);

/// <summary>
/// Menus ativos liberados ao usuário, ordenados. Administrador recebe todos. Usuário comum recebe as páginas
/// concedidas e, só para exibição, o agrupador de cada grupo em que tem ao menos uma página.
/// </summary>
public sealed record GetAllowedMenusQuery(Guid UserId) : IQuery<IReadOnlyList<MenuEntry>>;

public sealed class GetAllowedMenusHandler(AppDbContext db, IMemoryCache cache)
    : IQueryHandler<GetAllowedMenusQuery, IReadOnlyList<MenuEntry>>
{
    // A expiração é só uma rede de segurança; Grant/Revoke invalidam o cache na hora.
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<MenuEntry>> HandleAsync(GetAllowedMenusQuery query, CancellationToken cancellationToken = default)
    {
        var entries = await cache.GetOrCreateAsync(PermissionCache.Key(query.UserId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
            return await LoadAsync(query.UserId, cancellationToken);
        });

        return entries ?? [];
    }

    private async Task<IReadOnlyList<MenuEntry>> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        // O filtro global já exclui usuário removido logicamente: sem usuário, sem acesso.
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsAdmin })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return [];
        }

        var menus = db.MenuItems.Where(m => m.IsActive);

        if (!user.IsAdmin)
        {
            // Agrupadores (ex.: Cadastros) não são concedidos a ninguém: vêm junto quando o usuário tem ao menos
            // uma página do grupo. Sem isso a sidebar não saberia o título da seção e jogaria tudo em "Principal".
            var granted = db.UserMenuPermissions.Where(p => p.UserId == userId).Select(p => p.MenuItemId);

            menus = menus.Where(m =>
                granted.Contains(m.Id)
                || (m.Url == null && db.MenuItems.Any(child => child.ParentId == m.Id && child.IsActive && granted.Contains(child.Id))));
        }

        return await menus
            .OrderBy(m => m.SortOrder)
            .Select(m => new MenuEntry(m.Code, m.Description, m.Icon, m.Url, m.SortOrder, m.Parent != null ? m.Parent.Code : null))
            .ToListAsync(cancellationToken);
    }
}
