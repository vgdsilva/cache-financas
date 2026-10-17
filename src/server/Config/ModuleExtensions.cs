using System.Reflection;
using CacheFinancas.Server.Shared;

namespace CacheFinancas.Server.Config;

public static class ModuleExtensions
{
    private static readonly IModule[] Modules = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IModule).IsAssignableFrom(t))
        .Select(t => (IModule)Activator.CreateInstance(t)!)
        .ToArray();

    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var module in Modules)
            module.AddServices(services, configuration);
        return services;
    }

    public static WebApplication MapModules(this WebApplication app)
    {
        foreach (var module in Modules)
            module.MapEndpoints(app);
        return app;
    }
}
