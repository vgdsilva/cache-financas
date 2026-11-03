using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Pages.Configuracoes.Usuarios;

/// <summary>
/// Base das páginas de usuários. A regra "só administrador gerencia usuários" é imposta pelos handlers;
/// aqui só se decide o que mostrar (por isso consulta o perfil do usuário logado).
/// </summary>
public abstract class UsuariosPageModel(IQueryHandler<GetUserProfileQuery, UserProfile?> profile) : PageModel
{
    public Guid CurrentUserId => CurrentUser.GetId(User) ?? Guid.Empty;

    public bool IsAdmin { get; private set; }

    protected async Task<bool> LoadIsAdminAsync(CancellationToken cancellationToken)
    {
        var current = await profile.HandleAsync(new GetUserProfileQuery(CurrentUserId), cancellationToken);
        IsAdmin = current?.IsAdmin == true;

        return IsAdmin;
    }

    /// <summary>Volta para a listagem (URL vinda do catálogo de menus).</summary>
    protected IActionResult ToList() => LocalRedirect(Menus.UrlOf(Menus.Users)!);

    protected IActionResult ToAccessDenied() => RedirectToPage("/AccessDenied");

    /// <summary>Chave do <c>ModelState</c> do campo que o handler apontou no erro (cada página mapeia para os seus campos).</summary>
    protected virtual string FieldKey(string field) => $"Input.{field}";

    /// <summary>
    /// Mensagem de regra de negócio vira erro de formulário (junto do campo, quando o handler indica qual);
    /// qualquer outra exceção segue como erro real.
    /// </summary>
    protected async Task<bool> TryRunAsync(Func<Task> action)
    {
        try
        {
            await action();

            return true;
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(ex.Field is null ? string.Empty : FieldKey(ex.Field), ex.Message);

            return false;
        }
    }
}

/// <summary>
/// Campos do formulário de usuário (criar e editar). As regras de verdade (formato do login, e-mail, tamanhos)
/// ficam nos handlers; aqui só o que o formulário precisa para chegar até eles.
/// </summary>
public class UserInput
{
    [Display(Name = "Usuário")]
    [Required(ErrorMessage = "Informe o usuário.")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Nome completo")]
    public string? FullName { get; set; }

    /// <summary>Só usada ao criar. O tamanho mínimo é regra do handler.</summary>
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    public string? ConfirmPassword { get; set; }

    public bool IsAdmin { get; set; }

    public List<string> MenuCodes { get; set; } = [];
}

/// <summary>Base de Criar e Editar: mesmo formulário (<c>_Formulario</c>) e mesma lista de menus atribuíveis.</summary>
public abstract class UsuarioFormPageModel(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<ListAssignableMenusQuery, IReadOnlyList<AssignableMenu>> assignableMenus) : UsuariosPageModel(profile)
{
    [BindProperty]
    public UserInput Input { get; set; } = new();

    public IReadOnlyList<AssignableMenu> AvailableMenus { get; private set; } = [];

    public abstract bool IsEdit { get; }

    protected async Task LoadMenusAsync(CancellationToken cancellationToken)
        => AvailableMenus = await assignableMenus.HandleAsync(new ListAssignableMenusQuery(), cancellationToken);
}
