namespace VGS.FinanceIA.Framework.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Pipeline padrão: tratamento de erro, roteamento, autenticação e autorização (nessa ordem).</summary>
    public static WebApplication UseFramework(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>Endpoints públicos (estáticos e health) e os controllers MVC.</summary>
    public static WebApplication MapFramework(this WebApplication app)
    {
        app.MapStaticAssets().AllowAnonymous();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapControllers().WithStaticAssets();

        return app;
    }
}
