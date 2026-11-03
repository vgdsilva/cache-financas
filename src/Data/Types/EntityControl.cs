using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace VGS.FinanceIA.Data.Types;

public abstract class EntityControl
{
    [Column("id", TypeName = "uuid")]
    public Guid Id { get; set; } = Guid.CreateVersion7();


    [Column("create_at", TypeName = "timestamp with time zone")]
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;

    [Column("update_at", TypeName = "timestamp with time zone")]
    public DateTime UpdateAt { get; set; } = DateTime.UtcNow;



    /// <summary>Guid.Empty = ativo; excluído guarda o próprio Id (permite índice único só entre ativos).</summary>
    [Column("system_deleted", TypeName = "uuid")]
    public Guid? SystemDeleted { get; set; } = Guid.Empty;

    [Column("system_deleted_at", TypeName = "timestamp with time zone")]
    public DateTime? SystemDeletedAt { get; set; }

}
