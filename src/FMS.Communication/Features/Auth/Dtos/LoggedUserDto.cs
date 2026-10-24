namespace FMS.Communication.Features.Auth.Dtos;

public class LoggedUserDto
{
    public Guid   Id    { get; set; }
    public string Name  { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool IsAdmin            { get; set; }
    public bool MustChangePassword { get; set; }
    public bool TwoFactorEnabled   { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public List<UserPermissionDto> Permissions { get; set; } = [];
}

public class UserPermissionDto
{
    public Guid    MenuItemId   { get; set; }
    public Guid?   MenuItemSuperiorId { get; set; }
    public string  Code         { get; set; } = string.Empty;
    public string  Description  { get; set; } = string.Empty;
    public string? Icon         { get; set; }
    public string? URL          { get; set; }
    public string? Page         { get; set; }
    public int     Ordem        { get; set; }

    public bool CanView   { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit   { get; set; }
    public bool CanDelete { get; set; }
}
