namespace CacheFinancas.Server.Modules.Usuarios;

public record CriarUsuarioRequest(string Nome, string Email);

public record UsuarioResponse(Guid Id, string Nome, string Email, DateTime CriadoEm);
