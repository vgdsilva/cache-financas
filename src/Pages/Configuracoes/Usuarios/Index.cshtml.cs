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
    [BindProperty(Name = "q", SupportsGet = true)]
    public string? Search { get; set; }

    public IReadOnlyList<UserListItem> Users { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadIsAdminAsync(cancellationToken);
        Users = await list.HandleAsync(new ListUsersQuery(Search), cancellationToken);
    }
}
