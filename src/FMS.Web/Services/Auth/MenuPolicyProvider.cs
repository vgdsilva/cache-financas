using Microsoft.AspNetCore.Authorization;

namespace FMS.Web.Services.Auth;

/// <summary>
/// Cria policies sob demanda no formato "menu:CODIGO", exigindo que o menu esteja entre os liberados no token.
/// Uso: [Authorize(Policy = "menu:CONFIG.USUARIOS")]
/// </summary>
public class MenuPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    private const string Prefix = "menu:";

    public static string For(string menuCode) => Prefix + menuCode;

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("permission", policyName[Prefix.Length..])
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
