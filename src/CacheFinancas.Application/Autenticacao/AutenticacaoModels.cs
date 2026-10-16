using CacheFinancas.Domain.Enums;

namespace CacheFinancas.Application.Autenticacao;

public class LockoutOptions
{
    public const string Secao = "Autenticacao:Lockout";

    public int MaxTentativas { get; set; } = 5;
    public int DuracaoMinutos { get; set; } = 15;
}

public class BootstrapOptions
{
    public const string Secao = "Bootstrap";

    public string? AdminEmail { get; set; }
    public string? AdminSenha { get; set; }
}

public enum StatusLogin
{
    Sucesso,
    CredenciaisInvalidas,
    Bloqueado
}

public sealed record UsuarioAutenticado(Guid Id, string Nome, string Email, PerfilUsuario Perfil, bool DeveTrocarSenha);

public sealed record ResultadoLogin(StatusLogin Status, UsuarioAutenticado? Usuario = null);

public sealed record ResultadoOperacao(bool Sucesso, string? Erro = null)
{
    public static ResultadoOperacao Ok() => new(true);
    public static ResultadoOperacao Falha(string erro) => new(false, erro);
}
