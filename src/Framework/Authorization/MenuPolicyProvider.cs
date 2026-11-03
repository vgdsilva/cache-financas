using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace VGS.FinanceIA.Framework.Authorization;

/// <summary>Cria policies "menu:CODIGO" sob demanda: usuário autenticado + acesso ao menu.</summary>
public sealed class MenuPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequireMenuAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new MenuRequirement(policyName[RequireMenuAttribute.PolicyPrefix.Length..]))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
