using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VGS.FinanceIA.Data.Types;

// UserId nulo = configuração global. A regra "nulos contam como iguais" nos índices únicos
// é aplicada para todas as entidades no AppDbContext (não existe atributo para isso).
[Table("configuration")]
[Index(nameof(Key), nameof(UserId), nameof(SystemDeleted), IsUnique = true)]
public class Configuration : EntityControl
{
    [Column("key", TypeName = "text")]
    public string Key { get; set; } = string.Empty;

    [Column("userid", TypeName = "uuid")]
    public Guid? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("value", TypeName = "text")]
    public string Value { get; set; } = string.Empty;
}
