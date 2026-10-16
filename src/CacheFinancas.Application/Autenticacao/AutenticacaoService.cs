using CacheFinancas.Application.Abstractions;
using CacheFinancas.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CacheFinancas.Application.Autenticacao;

public class AutenticacaoService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    LockoutOptions lockout,
    TimeProvider relogio,
    ILogger<AutenticacaoService> logger) : IAutenticacaoService
{
    public async Task<ResultadoLogin> LoginAsync(string email, string senha, CancellationToken ct = default)
    {
        senha ??= string.Empty;
        var emailNormalizado = Usuario.NormalizarEmail(email ?? string.Empty);
        var usuario = await usuarios.ObterPorEmailAsync(emailNormalizado, ct);

        if (usuario is null || !usuario.PodeEntrar)
        {
            hasher.SimularVerificacao(senha);
            logger.LogWarning("Falha de login: usuário inexistente ou inativo ({Email})", emailNormalizado);
            return new ResultadoLogin(StatusLogin.CredenciaisInvalidas);
        }

        var agora = relogio.GetUtcNow().UtcDateTime;

        if (usuario.EstaBloqueado(agora))
        {
            logger.LogWarning("Login recusado: conta bloqueada ({UsuarioId}, {Email})", usuario.Id, usuario.Email);
            return new ResultadoLogin(StatusLogin.Bloqueado);
        }

        if (!hasher.Verificar(senha, usuario.SenhaHash))
        {
            usuario.RegistrarFalha(agora, lockout.MaxTentativas, TimeSpan.FromMinutes(lockout.DuracaoMinutos));
            await usuarios.SalvarAsync(ct);

            if (usuario.EstaBloqueado(agora))
                logger.LogWarning("Conta bloqueada por tentativas excessivas ({UsuarioId}, {Email})", usuario.Id, usuario.Email);
            else
                logger.LogWarning("Falha de login: senha incorreta ({UsuarioId}, {Email})", usuario.Id, usuario.Email);

            return new ResultadoLogin(StatusLogin.CredenciaisInvalidas);
        }

        usuario.RegistrarSucesso(agora);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Login realizado ({UsuarioId}, {Email})", usuario.Id, usuario.Email);

        return new ResultadoLogin(StatusLogin.Sucesso,
            new UsuarioAutenticado(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil, usuario.DeveTrocarSenha));
    }

    public async Task<ResultadoOperacao> TrocarSenhaAsync(Guid usuarioId, string senhaAtual, string novaSenha, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(novaSenha) || novaSenha.Length < Usuario.TamanhoMinimoSenha)
            return ResultadoOperacao.Falha($"A nova senha deve ter ao menos {Usuario.TamanhoMinimoSenha} caracteres.");

        var usuario = await usuarios.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null || !usuario.PodeEntrar)
            return ResultadoOperacao.Falha("Usuário não encontrado.");

        if (!hasher.Verificar(senhaAtual ?? string.Empty, usuario.SenhaHash))
            return ResultadoOperacao.Falha("Senha atual incorreta.");

        if (senhaAtual == novaSenha)
            return ResultadoOperacao.Falha("A nova senha deve ser diferente da atual.");

        usuario.DefinirSenha(hasher.Hash(novaSenha), deveTrocarSenha: false, relogio.GetUtcNow().UtcDateTime);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Senha alterada ({UsuarioId}, {Email})", usuario.Id, usuario.Email);

        return ResultadoOperacao.Ok();
    }
}
