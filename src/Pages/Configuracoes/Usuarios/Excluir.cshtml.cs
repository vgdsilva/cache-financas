using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages.Configuracoes.Usuarios;

[RequireMenu(Menus.Users)]
public class ExcluirModel(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<GetUserQuery, UserDetail?> getUser,
    ICommandHandler<DeleteUserCommand> delete) : UsuariosPageModel(profile)
{
    public Guid UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public bool IsSelf => UserId == CurrentUserId;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadIsAdminAsync(cancellationToken))
        {
            return ToAccessDenied();
        }

        return await LoadUserAsync(id, cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadIsAdminAsync(cancellationToken))
        {
            return ToAccessDenied();
        }

        if (!await LoadUserAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var deleted = await TryRunAsync(() => delete.HandleAsync(new DeleteUserCommand(CurrentUserId, id), cancellationToken));

        if (!deleted)
        {
            return Page();
        }

        TempData["Success"] = $"Usuário '{UserName}' excluído.";

        return ToList();
    }

    private async Task<bool> LoadUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await getUser.HandleAsync(new GetUserQuery(id), cancellationToken);

        if (user is null)
        {
            return false;
        }

        UserId = user.Id;
        UserName = user.UserName;

        return true;
    }
}
