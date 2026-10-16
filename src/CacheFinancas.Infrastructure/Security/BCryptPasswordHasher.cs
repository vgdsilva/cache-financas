using CacheFinancas.Application.Abstractions;

namespace CacheFinancas.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Hash fictício calculado uma vez; usado para igualar o tempo de resposta quando o usuário não existe.
    private static readonly string HashFicticio = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, WorkFactor);

    public bool Verificar(string senha, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public void SimularVerificacao(string senha) => BCrypt.Net.BCrypt.Verify(senha, HashFicticio);
}
