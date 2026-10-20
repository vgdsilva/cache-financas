namespace FMS.Domain.entities;

public partial class MenuItem : EntityControl
{
    public         Guid?    MenuItemSuperiorId     { get; set; }
    public virtual MenuItem MenuItemSuperiorEntity { get; set; }

    public string Description { get; set; }
    public string Icon        { get; set; }
    public string URL         { get; set; }
    public string Page        { get; set; }
    public int    Ordem       { get; set; }

}