namespace FMS.Communication.Features.Auth.Commands;

public record LoginCommand(
    string  Login,
    string  Password,
    string? IpAddress = null,
    string? UserAgent = null);
