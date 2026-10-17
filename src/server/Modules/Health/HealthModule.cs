using CacheFinancas.Server.Shared;

namespace CacheFinancas.Server.Modules.Health;

public class HealthModule : IModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration) { }

    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
}
