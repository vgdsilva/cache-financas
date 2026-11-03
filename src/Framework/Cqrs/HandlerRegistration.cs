using System.Reflection;

namespace VGS.FinanceIA.Framework.Cqrs;

public static class HandlerRegistration
{
    private static readonly Type[] _handlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>
    /// Registra (scoped) toda classe concreta que implementa ICommandHandler ou IQueryHandler no assembly.
    /// Handler novo não precisa de registro manual.
    /// </summary>
    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var implemented = type.GetInterfaces()
                .Where(i => i.IsGenericType && _handlerInterfaces.Contains(i.GetGenericTypeDefinition()));

            foreach (var serviceType in implemented)
            {
                services.AddScoped(serviceType, type);
            }
        }

        return services;
    }
}
