using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages.Configuracoes.Usuarios;

[RequireMenu(Menus.Users)]
public class SenhaModel(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<GetUserQuery, UserDetail?> getUser,
    ICommandHandler<ChangeUserPasswordCommand> changePassword) : UsuariosPageModel(profile)
{
    public class PasswordInput
    {
        [Required(ErrorMessage = "Informe a nova senha.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
        public string? ConfirmNewPassword { get; set; }
    }

    [BindProperty]
    public PasswordInput Input { get; set; } = new();

    public Guid UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

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

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var changed = await TryRunAsync(() => changePassword.HandleAsync(
            new ChangeUserPasswordCommand(CurrentUserId, id, Input.NewPassword),
            cancellationToken));

        if (!changed)
        {
            return Page();
        }

        TempData["Success"] = $"Senha de '{UserName}' redefinida.";

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
