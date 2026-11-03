using Microsoft.AspNetCore.Authentication.Cookies;

namespace VGS.FinanceIA.Framework.Authentication;

public static class CookieAuthenticationExtensions
{
    public static IServiceCollection AddFrameworkAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "financeia.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;

                // Requisições HTMX não devem receber o HTML do login dentro do swap: devolvem o status e pedem redirect.
                options.Events.OnRedirectToLogin = context =>
                    RedirectUnlessHtmx(context.Request, context.Response, context.RedirectUri, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context =>
                    RedirectUnlessHtmx(context.Request, context.Response, context.RedirectUri, StatusCodes.Status403Forbidden);
            });

        return services;
    }

    private static Task RedirectUnlessHtmx(HttpRequest request, HttpResponse response, string redirectUri, int htmxStatusCode)
    {
        if (request.Headers.ContainsKey("HX-Request"))
        {
            response.StatusCode = htmxStatusCode;
            response.Headers["HX-Redirect"] = redirectUri;
        }
        else
        {
            response.Redirect(redirectUri);
        }

        return Task.CompletedTask;
    }
}
