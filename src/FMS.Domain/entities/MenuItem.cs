namespace FMS.Domain.entities;

public partial class MenuItem : EntityControl
{
    public         Guid?     MenuItemSuperiorId     { get; set; }
    public virtual MenuItem? MenuItemSuperiorEntity { get; set; }

    public virtual ICollection<MenuItem>           Children    { get; set; } = new List<MenuItem>();
    public virtual ICollection<UserMenuPermission> Permissions { get; set; } = new List<UserMenuPermission>();

    /// <summary>Chave estável usada nas permissões e claims, ex.: FIN.LANCAMENTOS.</summary>
    public string  Code        { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    public string? Icon        { get; set; }
    public string? URL         { get; set; }
    public string? Page        { get; set; }
    public int     Ordem       { get; set; }
    public bool    IsActive    { get; set; } = true;
}
