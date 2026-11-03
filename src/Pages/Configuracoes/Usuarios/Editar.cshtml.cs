using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages.Configuracoes.Usuarios;

[RequireMenu(Menus.Users)]
public class EditarModel(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<ListAssignableMenusQuery, IReadOnlyList<AssignableMenu>> assignableMenus,
    IQueryHandler<GetUserQuery, UserDetail?> getUser,
    ICommandHandler<UpdateUserCommand> update) : UsuarioFormPageModel(profile, assignableMenus)
{
    public override bool IsEdit => true;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadIsAdminAsync(cancellationToken))
        {
            return ToAccessDenied();
        }

        var user = await getUser.HandleAsync(new GetUserQuery(id), cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        EditingUserId = user.Id;
        Input =new UserInput { UserName = user.UserName, Email = user.Email, FullName = user.FullName, IsAdmin = user.IsAdmin, MenuCodes = [.. user.MenuCodes] };
        await LoadMenusAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadIsAdminAsync(cancellationToken))
        {
            return ToAccessDenied();
        }

        EditingUserId = id;
        await LoadMenusAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var saved = await TryRunAsync(() => update.HandleAsync(
            new UpdateUserCommand(CurrentUserId, id, Input.UserName, Input.Email, Input.FullName, Input.IsAdmin, Input.MenuCodes),
            cancellationToken));

        if (!saved)
        {
            return Page();
        }

        TempData["Success"] = $"Usuário '{UserNames.Normalize(Input.UserName)}' atualizado.";

        return ToList();
    }
}
