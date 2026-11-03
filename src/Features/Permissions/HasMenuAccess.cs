using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Permissions;

/// <summary>Única pergunta "este usuário acessa este menu?" do sistema. Páginas e tools de IA usam esta query.</summary>
public sealed record HasMenuAccessQuery(Guid UserId, string MenuCode) : IQuery<bool>;

public sealed class HasMenuAccessHandler(IQueryHandler<GetAllowedMenusQuery, IReadOnlyList<MenuEntry>> allowedMenus)
    : IQueryHandler<HasMenuAccessQuery, bool>
{
    public async Task<bool> HandleAsync(HasMenuAccessQuery query, CancellationToken cancellationToken = default)
    {
        var menus = await allowedMenus.HandleAsync(new GetAllowedMenusQuery(query.UserId), cancellationToken);

        return menus.Any(m => string.Equals(m.Code, query.MenuCode, StringComparison.Ordinal));
    }
}
