using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Data.Types;

/// <summary>Menu do sistema. O catálogo vive em <c>Data/Catalog/Menus.cs</c> e é sincronizado pelo seeder.</summary>
[Table("menu_item")]
[Index(nameof(Code), nameof(SystemDeleted), IsUnique = true)]
public class MenuItem : EntityControl
{
    /// <summary>Chave estável usada em permissões e policies, ex.: FIN.TRANSACTIONS.</summary>
    [Column("code", TypeName = "text")]
    public string Code { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string Description { get; set; } = string.Empty;

    [Column("icon", TypeName = "text")]
    public string? Icon { get; set; }

    [Column("url", TypeName = "text")]
    public string? Url { get; set; }

    [Column("sort_order", TypeName = "integer")]
    public int SortOrder { get; set; }

    [Column("is_active", TypeName = "boolean")]
    public bool IsActive { get; set; } = true;

    [Column("parent_id", TypeName = "uuid")]
    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public MenuItem? Parent { get; set; }
}
