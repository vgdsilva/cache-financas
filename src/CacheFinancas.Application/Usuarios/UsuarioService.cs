using CacheFinancas.Application.Abstractions;
using CacheFinancas.Application.Autenticacao;
using CacheFinancas.Domain.Entities;
using CacheFinancas.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CacheFinancas.Application.Usuarios;

public sealed record UsuarioResumo(
    Guid Id, string Nome, string Email, PerfilUsuario Perfil, bool Ativo, bool Bloqueado, bool DeveTrocarSenha);

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct = default);
    Task<ResultadoOperacao> CriarAsync(string nome, string email, string senhaTemporaria, PerfilUsuario perfil, CancellationToken ct = default);
    Task<ResultadoOperacao> AlterarAtivoAsync(Guid alvoId, bool ativo, Guid solicitanteId, CancellationToken ct = default);
    Task<ResultadoOperacao> RedefinirSenhaAsync(Guid alvoId, string senhaTemporaria, CancellationToken ct = default);
    Task<ResultadoOperacao> DesbloquearAsync(Guid alvoId, CancellationToken ct = default);
}

public class UsuarioService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    TimeProvider relogio,
    ILogger<UsuarioService> logger) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioResumo>> ListarAsync(CancellationToken ct = default)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var lista = await usuarios.ListarAsync(ct);
        return lista
            .Select(u => new UsuarioResumo(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, u.EstaBloqueado(agora), u.DeveTrocarSenha))
            .ToList();
    }

    public async Task<ResultadoOperacao> CriarAsync(string nome, string email, string senhaTemporaria, PerfilUsuario perfil, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome)) return ResultadoOperacao.Falha("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return ResultadoOperacao.Falha("E-mail inválido.");
        if (string.IsNullOrEmpty(senhaTemporaria) || senhaTemporaria.Length < Usuario.TamanhoMinimoSenha)
            return ResultadoOperacao.Falha($"A senha deve ter ao menos {Usuario.TamanhoMinimoSenha} caracteres.");

        var emailNormalizado = Usuario.NormalizarEmail(email);
        if (await usuarios.ObterPorEmailAsync(emailNormalizado, ct) is not null)
            return ResultadoOperacao.Falha("Já existe um usuário com este e-mail.");

        var usuario = Usuario.Criar(nome, emailNormalizado, hasher.Hash(senhaTemporaria), perfil, deveTrocarSenha: true);
        await usuarios.AdicionarAsync(usuario, ct);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Usuário criado ({UsuarioId}, {Email}, {Perfil})", usuario.Id, usuario.Email, usuario.Perfil);

        return ResultadoOperacao.Ok();
    }

    public async Task<ResultadoOperacao> AlterarAtivoAsync(Guid alvoId, bool ativo, Guid solicitanteId, CancellationToken ct = default)
    {
        if (!ativo && alvoId == solicitanteId)
            return ResultadoOperacao.Falha("Você não pode desativar a própria conta.");

        var usuario = await usuarios.ObterPorIdAsync(alvoId, ct);
        if (usuario is null) return ResultadoOperacao.Falha("Usuário não encontrado.");

        usuario.DefinirAtivo(ativo, relogio.GetUtcNow().UtcDateTime);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Usuário {Acao} ({UsuarioId}, {Email}) por {SolicitanteId}",
            ativo ? "ativado" : "desativado", usuario.Id, usuario.Email, solicitanteId);

        return ResultadoOperacao.Ok();
    }

    public async Task<ResultadoOperacao> RedefinirSenhaAsync(Guid alvoId, string senhaTemporaria, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(senhaTemporaria) || senhaTemporaria.Length < Usuario.TamanhoMinimoSenha)
            return ResultadoOperacao.Falha($"A senha deve ter ao menos {Usuario.TamanhoMinimoSenha} caracteres.");

        var usuario = await usuarios.ObterPorIdAsync(alvoId, ct);
        if (usuario is null) return ResultadoOperacao.Falha("Usuário não encontrado.");

        var agora = relogio.GetUtcNow().UtcDateTime;
        usuario.DefinirSenha(hasher.Hash(senhaTemporaria), deveTrocarSenha: true, agora);
        usuario.Desbloquear(agora);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Senha redefinida pelo admin ({UsuarioId}, {Email})", usuario.Id, usuario.Email);

        return ResultadoOperacao.Ok();
    }

    public async Task<ResultadoOperacao> DesbloquearAsync(Guid alvoId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorIdAsync(alvoId, ct);
        if (usuario is null) return ResultadoOperacao.Falha("Usuário não encontrado.");

        usuario.Desbloquear(relogio.GetUtcNow().UtcDateTime);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Conta desbloqueada ({UsuarioId}, {Email})", usuario.Id, usuario.Email);

        return ResultadoOperacao.Ok();
    }
}
