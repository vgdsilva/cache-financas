using Microsoft.Extensions.DependencyInjection;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Framework.Cqrs;

public class HandlerRegistrationTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public void AddHandlers_RegistersEveryHandlerImplementationInTheAssembly()
    {
        var services = new ServiceCollection();

        services.AddHandlers(typeof(Program).Assembly);

        var handlerTypes = typeof(Program).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.Namespace == typeof(ICommand).Namespace))
            .ToList();

        Assert.NotEmpty(handlerTypes);
        Assert.All(handlerTypes, type => Assert.Contains(services, d => d.ImplementationType == type));
    }

    [Fact]
    public void EveryHandler_ResolvesFromTheRealContainer()
    {
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        // Cobre o grafo de dependências de cada handler (um erro de registro só apareceria em runtime).
        Assert.NotNull(provider.GetRequiredService<IQueryHandler<GetAllowedMenusQuery, IReadOnlyList<MenuEntry>>>());
        Assert.NotNull(provider.GetRequiredService<IQueryHandler<HasMenuAccessQuery, bool>>());
        Assert.NotNull(provider.GetRequiredService<ICommandHandler<GrantMenuAccessCommand>>());
        Assert.NotNull(provider.GetRequiredService<ICommandHandler<RevokeMenuAccessCommand>>());
        Assert.NotNull(provider.GetRequiredService<ICommandHandler<CreateUserCommand, Guid>>());
        Assert.NotNull(provider.GetRequiredService<ICommandHandler<AuthenticateUserCommand, AuthenticatedUser?>>());
        Assert.NotNull(provider.GetRequiredService<ICommandHandler<EnsureInitialAdminCommand, bool>>());
    }
}
