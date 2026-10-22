namespace FMS.Domain.entities;

public partial class User : EntityControl
{
    public string Name  { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive           { get; set; } = true;
    public bool IsAdmin            { get; set; } = false;
    public bool MustChangePassword { get; set; } = false;

    public int       FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil    { get; set; }
    public DateTime? LastLoginAt    { get; set; }

    public bool    TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret  { get; set; }

    public virtual ICollection<UserSession>        Sessions    { get; set; } = new List<UserSession>();
    public virtual ICollection<UserMenuPermission> Permissions { get; set; } = new List<UserMenuPermission>();
}
