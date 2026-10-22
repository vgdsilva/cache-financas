namespace FMS.Domain.entities;

public partial class UserLoginAttempt : EntityControl
{
    public Guid?   UserId    { get; set; }
    public string  Login     { get; set; } = string.Empty;
    public bool    Success   { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
