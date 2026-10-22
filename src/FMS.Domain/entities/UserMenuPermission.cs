namespace FMS.Domain.entities;

/// <summary>Permissão de um usuário em um menu. Único por (UserId, MenuItemId).</summary>
public partial class UserMenuPermission : EntityControl
{
    public         Guid  UserId     { get; set; }
    public virtual User? UserEntity { get; set; }

    public         Guid      MenuItemId     { get; set; }
    public virtual MenuItem? MenuItemEntity { get; set; }

    public bool CanView   { get; set; } = true;
    public bool CanCreate { get; set; } = false;
    public bool CanEdit   { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}
