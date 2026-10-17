using CacheFinancas.Server.Shared;

namespace CacheFinancas.Server.Modules.Usuarios;

public class UsuarioModule : IModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<UsuarioService>();

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/usuarios");

        g.MapGet("/", async (UsuarioService s, CancellationToken ct) => Results.Ok(await s.ListarAsync(ct)));

        g.MapGet("/{id:guid}", async (Guid id, UsuarioService s, CancellationToken ct) =>
            await s.ObterAsync(id, ct) is { } u ? Results.Ok(u) : Results.NotFound());

        g.MapPost("/", async (CriarUsuarioRequest req, UsuarioService s, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Nome) || string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["Nome e e-mail validos sao obrigatorios."]
                });

            return await s.CriarAsync(req, ct) is { } criado
                ? Results.Created($"/usuarios/{criado.Id}", criado)
                : Results.Conflict(new { title = "E-mail ja cadastrado" });
        });
    }
}
