namespace CacheFinancas.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);

    /// <summary>Gasta o mesmo tempo de uma verificação real (mitiga enumeração de e-mails por tempo).</summary>
    void SimularVerificacao(string senha);
}
