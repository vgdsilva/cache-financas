using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages.Configuracoes.Usuarios;

[RequireMenu(Menus.Users)]
public class IndexModel(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<ListUsersQuery, IReadOnlyList<UserListItem>> list) : UsuariosPageModel(profile)
{
    public IReadOnlyList<UserListItem> Users { get; private set; } = [];

    /// <summary>Traz todos os usuários; os filtros por coluna e a paginação da tabela rodam no navegador (site.js).</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadIsAdminAsync(cancellationToken);
        Users = await list.HandleAsync(new ListUsersQuery(), cancellationToken);
    }
}
