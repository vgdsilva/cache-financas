using CacheFinancas.Server.Shared;

namespace CacheFinancas.Server.Modules.Usuarios;

public class Usuario : Entity
{
    private Usuario() { }

    public Usuario(string nome, string email)
    {
        Nome = nome;
        Email = email;
    }

    public string Nome { get; private set; } = null!;

    public string Email { get; private set; } = null!;
}
