using CacheFinancas.Application.Abstractions;
using CacheFinancas.Application.Autenticacao;
using CacheFinancas.Application.Usuarios;
using CacheFinancas.Infrastructure.Persistence;
using CacheFinancas.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CacheFinancas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCacheFinancas(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton(config.GetSection(LockoutOptions.Secao).Get<LockoutOptions>() ?? new LockoutOptions());
        services.AddSingleton(config.GetSection(BootstrapOptions.Secao).Get<BootstrapOptions>() ?? new BootstrapOptions());

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAutenticacaoService, AutenticacaoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<AdminBootstrapper>();

        return services;
    }
}
