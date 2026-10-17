namespace CacheFinancas.Server.Config;

public static class CorsExtensions
{
    private const string Policy = "web";

    public static IServiceCollection AddWebCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        return services.AddCors(o => o.AddPolicy(Policy, p => p
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
    }

    public static WebApplication UseWebCors(this WebApplication app)
    {
        app.UseCors(Policy);
        return app;
    }
}
