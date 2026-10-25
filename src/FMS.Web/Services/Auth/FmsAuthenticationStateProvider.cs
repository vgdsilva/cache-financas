using System.Security.Claims;
using System.Text;
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

    /// <summary>Menus liberados ao usuário, lidos do claim "menus" do JWT.</summary>
    public IReadOnlyList<MenuClaim> Menus { get; private set; } = [];

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureLoadedAsync();

        if (_session is null)
        {
            Menus = [];
            return Anonymous;
        }

        if (_session.AccessExpiresAt <= DateTime.UtcNow)
        {
            await SignOutAsync();
            return Anonymous;
        }

        Menus = ReadMenus(_session.AccessToken);
        return new AuthenticationState(CreatePrincipal(_session.User, Menus));
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
        Menus = [];
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

    private static ClaimsPrincipal CreatePrincipal(LoggedUserDto user, IEnumerable<MenuClaim> menus)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        claims.AddRange(menus.Where(m => m.CanView).Select(m => new Claim("permission", m.Code)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt"));
    }

    /// <summary>Lê o claim de menus direto do payload do JWT (a assinatura é validada pela API).</summary>
    private static List<MenuClaim> ReadMenus(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return [];

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return doc.RootElement.TryGetProperty(MenuClaims.ClaimType, out var menus)
                ? MenuClaims.Deserialize(menus.GetString())
                : [];
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return [];
        }
    }
}
