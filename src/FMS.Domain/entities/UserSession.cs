namespace FMS.Domain.entities;

/// <summary>
/// Sessão de login. O Id vai na claim "sid" do JWT; o refresh token é guardado apenas como hash.
/// </summary>
public partial class UserSession : EntityControl
{
    public         Guid  UserId     { get; set; }
    public virtual User? UserEntity { get; set; }

    public string   RefreshTokenHash { get; set; } = string.Empty;
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime LastActivityAt   { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool      IsRevoked     { get; set; } = false;
    public DateTime? RevokedAt     { get; set; }
    public string?   RevokedReason { get; set; }
}
