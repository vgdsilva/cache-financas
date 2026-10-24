using System.Net;
using System.Net.Http.Headers;

namespace FMS.Web.Services.Auth;

/// <summary>Anexa o access token nas chamadas à API e encerra a sessão se a API responder 401 com token enviado.</summary>
public class AuthorizationMessageHandler(FmsAuthenticationStateProvider authState) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = authState.AccessToken;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
            await authState.SignOutAsync();

        return response;
    }
}
