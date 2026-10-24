using System.Net;
using System.Net.Http.Json;
using FMS.Communication.Features.Auth.Dtos;

namespace FMS.Web.Services.Auth;

public record LoginOutcome(bool Success, string? Error = null);

public class AuthService(HttpClient http, FmsAuthenticationStateProvider authState)
{
    public async Task<LoginOutcome> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                "auth/login",
                new LoginRequestDto { Login = login, Password = password },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken);
                if (result is null)
                    return new LoginOutcome(false, "Resposta inválida do servidor.");

                await authState.SignInAsync(result);
                return new LoginOutcome(true);
            }

            return new LoginOutcome(false, response.StatusCode switch
            {
                HttpStatusCode.BadRequest   => "Informe login e senha.",
                HttpStatusCode.Unauthorized => "Login ou senha inválidos.",
                HttpStatusCode.Locked       => "Usuário bloqueado temporariamente. Tente novamente em alguns minutos.",
                _                           => "Não foi possível entrar agora. Tente novamente."
            });
        }
        catch (HttpRequestException)
        {
            return new LoginOutcome(false, "Não foi possível conectar ao servidor.");
        }
    }

    public Task LogoutAsync() => authState.SignOutAsync();
}
