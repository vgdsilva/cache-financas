namespace CacheFinancas.Server.Shared;

/// <summary>
/// Contrato de um modulo (entidade/feature). Toda classe que implementa IModule
/// e descoberta automaticamente em Config/ModuleExtensions.cs, entao criar um
/// modulo novo nao exige editar o Program.cs.
/// </summary>
public interface IModule
{
    void AddServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder app);
}
