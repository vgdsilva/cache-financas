namespace CacheFinancas.Application.Autenticacao;

public interface IAutenticacaoService
{
    Task<ResultadoLogin> LoginAsync(string email, string senha, CancellationToken ct = default);
    Task<ResultadoOperacao> TrocarSenhaAsync(Guid usuarioId, string senhaAtual, string novaSenha, CancellationToken ct = default);
}
