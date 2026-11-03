using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Permissions;

/// <summary>Menu que pode ser concedido a um usuário. <paramref name="Group"/> é o título da seção na sidebar.</summary>
public sealed record AssignableMenu(string Code, string Description, string Group);

/// <summary>Menus ativos que são páginas, na ordem da sidebar (seção "Principal" primeiro, depois os grupos).</summary>
public sealed record ListAssignableMenusQuery : IQuery<IReadOnlyList<AssignableMenu>>;

public sealed class ListAssignableMenusHandler(AppDbContext db) : IQueryHandler<ListAssignableMenusQuery, IReadOnlyList<AssignableMenu>>
{
    public async Task<IReadOnlyList<AssignableMenu>> HandleAsync(ListAssignableMenusQuery query, CancellationToken cancellationToken = default)
    {
        var menus = await db.MenuItems
            .Where(m => m.IsActive && m.Url != null)
            .Include(m => m.Parent)
            .ToListAsync(cancellationToken);

        return menus
            .OrderBy(m => m.Parent is null ? 0 : 1)
            .ThenBy(m => m.Parent?.SortOrder ?? 0)
            .ThenBy(m => m.SortOrder)
            .Select(m => new AssignableMenu(m.Code, m.Description, m.Parent?.Description ?? Menus.MainSectionTitle))
            .ToList();
    }
}
