using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Data.Types;

[Table("users")]
[Index(nameof(UserName), nameof(SystemDeleted), IsUnique = true)]
public class User : EntityControl
{
    /// <summary>Login. Sempre em minúsculas; só letras, dígitos, ponto, hífen e underscore (3 a 40 caracteres).</summary>
    [Column("user_name", TypeName = "text")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Sempre em minúsculas. Obrigatório para quem é cadastrado pela tela; usuários antigos podem estar vazios até a próxima edição.</summary>
    [Column("email", TypeName = "text")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Opcional. Aparece em cabeçalhos e listas no lugar do login.</summary>
    [Column("full_name", TypeName = "text")]
    public string? FullName { get; set; }

    [Column("password_hash", TypeName = "text")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Administrador acessa todos os menus ativos, sem linhas em user_menu_permission.</summary>
    [Column("is_admin", TypeName = "boolean")]
    public bool IsAdmin { get; set; }
}
