using System.ComponentModel.DataAnnotations;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;

namespace VGS.FinanceIA.ViewModels;

/// <summary>Base das telas de usuários: quem está logado e se é administrador (só para decidir o que mostrar).</summary>
public abstract class UsuariosViewModel
{
    public Guid CurrentUserId { get; set; }

    public bool IsAdmin { get; set; }
}

public sealed class UsuariosIndexViewModel : UsuariosViewModel
{
    public IReadOnlyList<UserListItem> Users { get; set; } = [];
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

/// <summary>Criar e Editar: mesmo formulário (<c>_Formulario</c>) e mesma lista de menus atribuíveis.</summary>
public sealed class UsuarioFormViewModel : UsuariosViewModel
{
    public UserInput Input { get; set; } = new();

    public IReadOnlyList<AssignableMenu> AvailableMenus { get; set; } = [];

    public bool IsEdit { get; set; }

    /// <summary>Usuário em edição (só no Editar); o formulário usa para oferecer o atalho de redefinir a senha.</summary>
    public Guid? EditingUserId { get; set; }
}

public sealed class PasswordInput
{
    [Required(ErrorMessage = "Informe a nova senha.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
    public string? ConfirmNewPassword { get; set; }
}

/// <summary>Telas que agem sobre um usuário específico (redefinir senha, excluir).</summary>
public abstract class UsuarioAlvoViewModel : UsuariosViewModel
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;
}

public sealed class SenhaViewModel : UsuarioAlvoViewModel
{
    public PasswordInput Input { get; set; } = new();
}

public sealed class ExcluirViewModel : UsuarioAlvoViewModel
{
    public bool IsSelf => UserId == CurrentUserId;
}
