namespace FMS.Communication.Features.Auth.Dtos;

public class LoginResultDto
{
    public string   AccessToken      { get; set; } = string.Empty;
    public DateTime AccessExpiresAt  { get; set; }
    public string   RefreshToken     { get; set; } = string.Empty;
    public DateTime RefreshExpiresAt { get; set; }

    public LoggedUserDto User { get; set; } = new();
}
