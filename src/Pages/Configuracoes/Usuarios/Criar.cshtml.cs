using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages.Configuracoes.Usuarios;

[RequireMenu(Menus.Users)]
public class CriarModel(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<ListAssignableMenusQuery, IReadOnlyList<AssignableMenu>> assignableMenus,
    ICommandHandler<RegisterUserCommand, Guid> register) : UsuarioFormPageModel(profile, assignableMenus)
{
    public override bool IsEdit => false;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadIsAdminAsync(cancellationToken))
        {
            return ToAccessDenied();
        }

        await LoadMenusAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadIsAdminAsync(cancellationToken))
        {
            return ToAccessDenied();
        }

        await LoadMenusAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var created = await TryRunAsync(() => register.HandleAsync(
            new RegisterUserCommand(CurrentUserId, Input.UserName, Input.Password ?? string.Empty, Input.IsAdmin, Input.MenuCodes),
            cancellationToken));

        if (!created)
        {
            return Page();
        }

        TempData["Success"] = $"Usuário '{UserNames.Normalize(Input.UserName)}' criado.";

        return ToList();
    }
}
