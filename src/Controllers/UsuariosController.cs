using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.ViewModels;

namespace VGS.FinanceIA.Controllers;

/// <summary>
/// Cadastro de usuários. A regra "só administrador gerencia usuários" é imposta pelos handlers;
/// aqui só se decide o que mostrar (por isso consulta o perfil do usuário logado).
/// </summary>
[RequireMenu(Menus.Users)]
[Route("configuracoes/usuarios")]
public class UsuariosController(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<ListUsersQuery, IReadOnlyList<UserListItem>> list,
    IQueryHandler<GetUserQuery, UserDetail?> getUser,
    IQueryHandler<ListAssignableMenusQuery, IReadOnlyList<AssignableMenu>> assignableMenus,
    ICommandHandler<RegisterUserCommand, Guid> register,
    ICommandHandler<UpdateUserCommand> update,
    ICommandHandler<ChangeUserPasswordCommand> changePassword,
    ICommandHandler<DeleteUserCommand> delete) : Controller
{
    private Guid CurrentUserId => CurrentUser.GetId(User) ?? Guid.Empty;

    /// <summary>Traz todos os usuários; os filtros por coluna e a paginação da tabela rodam no navegador (site.js).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new UsuariosIndexViewModel();
        await LoadAdminAsync(model, cancellationToken);
        model.Users = await list.HandleAsync(new ListUsersQuery(), cancellationToken);

        return View(model);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Criar(CancellationToken cancellationToken)
    {
        var model = new UsuarioFormViewModel();

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        model.AvailableMenus = await assignableMenus.HandleAsync(new ListAssignableMenusQuery(), cancellationToken);

        return View(model);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Criar([Bind(Prefix = "Input")] UserInput input, CancellationToken cancellationToken)
    {
        var model = new UsuarioFormViewModel { Input = input };

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        model.AvailableMenus = await assignableMenus.HandleAsync(new ListAssignableMenusQuery(), cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var created = await TryRunAsync(() => register.HandleAsync(
            new RegisterUserCommand(CurrentUserId, input.UserName, input.Email, input.FullName, input.Password ?? string.Empty, input.IsAdmin, input.MenuCodes),
            cancellationToken));

        if (!created)
        {
            return View(model);
        }

        TempData["Success"] = $"Usuário '{UserNames.Normalize(input.UserName)}' criado.";

        return ToList();
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var model = new UsuarioFormViewModel { IsEdit = true };

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        var user = await getUser.HandleAsync(new GetUserQuery(id), cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        model.EditingUserId = user.Id;
        model.Input = new UserInput { UserName = user.UserName, Email = user.Email, FullName = user.FullName, IsAdmin = user.IsAdmin, MenuCodes = [.. user.MenuCodes] };
        model.AvailableMenus = await assignableMenus.HandleAsync(new ListAssignableMenusQuery(), cancellationToken);

        return View(model);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [Bind(Prefix = "Input")] UserInput input, CancellationToken cancellationToken)
    {
        var model = new UsuarioFormViewModel { IsEdit = true, EditingUserId = id, Input = input };

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        model.AvailableMenus = await assignableMenus.HandleAsync(new ListAssignableMenusQuery(), cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var saved = await TryRunAsync(() => update.HandleAsync(
            new UpdateUserCommand(CurrentUserId, id, input.UserName, input.Email, input.FullName, input.IsAdmin, input.MenuCodes),
            cancellationToken));

        if (!saved)
        {
            return View(model);
        }

        TempData["Success"] = $"Usuário '{UserNames.Normalize(input.UserName)}' atualizado.";

        return ToList();
    }

    [HttpGet("password/{id:guid}")]
    public async Task<IActionResult> Senha(Guid id, CancellationToken cancellationToken)
    {
        var model = new SenhaViewModel();

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        return await LoadUserAsync(model, id, cancellationToken) ? View(model) : NotFound();
    }

    [HttpPost("password/{id:guid}")]
    public async Task<IActionResult> Senha(Guid id, [Bind(Prefix = "Input")] PasswordInput input, CancellationToken cancellationToken)
    {
        var model = new SenhaViewModel { Input = input };

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        if (!await LoadUserAsync(model, id, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // O handler chama o campo de "Password"; nesta tela ele é o NewPassword.
        var changed = await TryRunAsync(
            () => changePassword.HandleAsync(new ChangeUserPasswordCommand(CurrentUserId, id, input.NewPassword), cancellationToken),
            field => field == "Password" ? "Input.NewPassword" : $"Input.{field}");

        if (!changed)
        {
            return View(model);
        }

        TempData["Success"] = $"Senha de '{model.UserName}' redefinida.";

        return ToList();
    }

    [HttpGet("delete/{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var model = new ExcluirViewModel();

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        return await LoadUserAsync(model, id, cancellationToken) ? View(model) : NotFound();
    }

    [HttpPost("delete/{id:guid}")]
    [ActionName("Excluir")]
    public async Task<IActionResult> ExcluirConfirmado(Guid id, CancellationToken cancellationToken)
    {
        var model = new ExcluirViewModel();

        if (!await LoadAdminAsync(model, cancellationToken))
        {
            return ToAccessDenied();
        }

        if (!await LoadUserAsync(model, id, cancellationToken))
        {
            return NotFound();
        }

        var deleted = await TryRunAsync(() => delete.HandleAsync(new DeleteUserCommand(CurrentUserId, id), cancellationToken));

        if (!deleted)
        {
            return View(model);
        }

        TempData["Success"] = $"Usuário '{model.UserName}' excluído.";

        return ToList();
    }

    private async Task<bool> LoadAdminAsync(UsuariosViewModel model, CancellationToken cancellationToken)
    {
        model.CurrentUserId = CurrentUserId;

        var current = await profile.HandleAsync(new GetUserProfileQuery(CurrentUserId), cancellationToken);
        model.IsAdmin = current?.IsAdmin == true;

        return model.IsAdmin;
    }

    private async Task<bool> LoadUserAsync(UsuarioAlvoViewModel model, Guid id, CancellationToken cancellationToken)
    {
        var user = await getUser.HandleAsync(new GetUserQuery(id), cancellationToken);

        if (user is null)
        {
            return false;
        }

        model.UserId = user.Id;
        model.UserName = user.UserName;

        return true;
    }

    /// <summary>Volta para a listagem (URL vinda do catálogo de menus).</summary>
    private IActionResult ToList() => LocalRedirect(Menus.UrlOf(Menus.Users)!);

    private IActionResult ToAccessDenied() => RedirectToAction("AccessDenied", "Home");

    /// <summary>
    /// Mensagem de regra de negócio vira erro de formulário (junto do campo, quando o handler indica qual);
    /// qualquer outra exceção segue como erro real.
    /// </summary>
    private async Task<bool> TryRunAsync(Func<Task> action, Func<string, string>? fieldKey = null)
    {
        try
        {
            await action();

            return true;
        }
        catch (BusinessRuleException ex)
        {
            var key = ex.Field is null ? string.Empty : (fieldKey ?? (field => $"Input.{field}"))(ex.Field);
            ModelState.AddModelError(key, ex.Message);

            return false;
        }
    }
}
