using System.Security.Claims;
using System.Text.Json;
using FMS.Communication.Features.Auth.Dtos;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace FMS.Web.Services.Auth;

/// <summary>
/// Guarda a sessão do usuário (token + dados) no localStorage e expõe o estado de login ao Blazor.
/// </summary>
public class FmsAuthenticationStateProvider(IJSRuntime js) : AuthenticationStateProvider
{
    private const string StorageKey = "fms.session";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private LoginResultDto? _session;
    private bool _loaded;

    public LoginResultDto? Session => _session;
    public string? AccessToken => _session?.AccessToken;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureLoadedAsync();

        if (_session is null)
            return Anonymous;

        if (_session.AccessExpiresAt <= DateTime.UtcNow)
        {
            await SignOutAsync();
            return Anonymous;
        }

        return new AuthenticationState(CreatePrincipal(_session.User));
    }

    public async Task SignInAsync(LoginResultDto session)
    {
        _session = session;
        _loaded = true;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(session));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        _session = null;
        _loaded = true;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded)
            return;

        _loaded = true;
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
                _session = JsonSerializer.Deserialize<LoginResultDto>(json);
        }
        catch
        {
            // Sessão ilegível ou localStorage indisponível: segue como deslogado.
            _session = null;
        }
    }

    private static ClaimsPrincipal CreatePrincipal(LoggedUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        claims.AddRange(user.Permissions.Where(p => p.CanView).Select(p => new Claim("permission", p.Code)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt"));
    }
}
