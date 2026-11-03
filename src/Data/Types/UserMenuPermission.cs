using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Data.Types;

/// <summary>Linha ativa = o usuário tem acesso ao menu. Revogar = excluir (exclusão lógica).</summary>
[Table("user_menu_permission")]
[Index(nameof(UserId), nameof(MenuItemId), nameof(SystemDeleted), IsUnique = true)]
public class UserMenuPermission : EntityControl
{
    [Column("user_id", TypeName = "uuid")]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("menu_item_id", TypeName = "uuid")]
    public Guid MenuItemId { get; set; }

    [ForeignKey(nameof(MenuItemId))]
    public MenuItem? MenuItem { get; set; }
}
