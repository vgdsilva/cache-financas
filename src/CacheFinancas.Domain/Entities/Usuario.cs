using CacheFinancas.Domain.Abstractions;
using CacheFinancas.Domain.Enums;

namespace CacheFinancas.Domain.Entities;

public class Usuario : EntityControl
{
    public const int TamanhoMinimoSenha = 10;

    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int TentativasFalhas { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }
    public bool DeveTrocarSenha { get; private set; }

    private Usuario() { }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    public static Usuario Criar(string nome, string email, string senhaHash, PerfilUsuario perfil, bool deveTrocarSenha)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("E-mail é obrigatório.", nameof(email));
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new ArgumentException("Hash da senha é obrigatório.", nameof(senhaHash));

        return new Usuario
        {
            Nome = nome.Trim(),
            Email = NormalizarEmail(email),
            SenhaHash = senhaHash,
            Perfil = perfil,
            DeveTrocarSenha = deveTrocarSenha
        };
    }

    public bool PodeEntrar => Ativo && SystemDeleted is null;

    public bool EstaBloqueado(DateTime agoraUtc) => BloqueadoAte.HasValue && BloqueadoAte.Value > agoraUtc;

    /// <summary>Registra uma senha incorreta; ao atingir o limite, bloqueia e reinicia o contador.</summary>
    public void RegistrarFalha(DateTime agoraUtc, int maxTentativas, TimeSpan duracaoBloqueio)
    {
        TentativasFalhas++;
        if (TentativasFalhas >= maxTentativas)
        {
            BloqueadoAte = agoraUtc.Add(duracaoBloqueio);
            TentativasFalhas = 0;
        }
        UpdatedAt = agoraUtc;
    }

    public void RegistrarSucesso(DateTime agoraUtc)
    {
        TentativasFalhas = 0;
        BloqueadoAte = null;
        UpdatedAt = agoraUtc;
    }

    public void Desbloquear(DateTime agoraUtc) => RegistrarSucesso(agoraUtc);

    public void DefinirSenha(string novoHash, bool deveTrocarSenha, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(novoHash)) throw new ArgumentException("Hash da senha é obrigatório.", nameof(novoHash));
        SenhaHash = novoHash;
        DeveTrocarSenha = deveTrocarSenha;
        UpdatedAt = agoraUtc;
    }

    public void DefinirAtivo(bool ativo, DateTime agoraUtc)
    {
        Ativo = ativo;
        UpdatedAt = agoraUtc;
    }
}
