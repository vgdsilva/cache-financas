using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Data.Types;

[Table("users")]
[Index(nameof(UserName), nameof(SystemDeleted), IsUnique = true)]
public class User : EntityControl
{
    /// <summary>Sempre em minúsculas (normalizado pelo UserService).</summary>
    [Column("user_name", TypeName = "text")]
    public string UserName { get; set; } = string.Empty;

    [Column("password_hash", TypeName = "text")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Administrador acessa todos os menus ativos, sem linhas em user_menu_permission.</summary>
    [Column("is_admin", TypeName = "boolean")]
    public bool IsAdmin { get; set; }
}
