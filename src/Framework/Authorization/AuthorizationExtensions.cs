using Microsoft.AspNetCore.Authorization;

namespace VGS.FinanceIA.Framework.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddFrameworkAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Negado por padrão: toda página exige usuário autenticado, exceto as marcadas com [AllowAnonymous].
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });

        services.AddSingleton<IAuthorizationPolicyProvider, MenuPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, MenuAuthorizationHandler>();

        return services;
    }
}
