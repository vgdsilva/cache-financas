using CacheFinancas.Domain.Entities;
using CacheFinancas.Domain.Enums;

namespace CacheFinancas.UnitTests;

public class UsuarioTests
{
    private static readonly DateTime Agora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Usuario Novo() => Usuario.Criar("Ana", "  Ana@Exemplo.COM ", "hash", PerfilUsuario.Usuario, true);

    [Fact]
    public void Criar_normaliza_email()
    {
        Assert.Equal("ana@exemplo.com", Novo().Email);
    }

    [Fact]
    public void Quatro_falhas_nao_bloqueiam()
    {
        var u = Novo();
        for (var i = 0; i < 4; i++) u.RegistrarFalha(Agora, 5, TimeSpan.FromMinutes(15));

        Assert.False(u.EstaBloqueado(Agora));
        Assert.Equal(4, u.TentativasFalhas);
    }

    [Fact]
    public void Quinta_falha_bloqueia_por_15_minutos()
    {
        var u = Novo();
        for (var i = 0; i < 5; i++) u.RegistrarFalha(Agora, 5, TimeSpan.FromMinutes(15));

        Assert.True(u.EstaBloqueado(Agora.AddMinutes(14)));
        Assert.False(u.EstaBloqueado(Agora.AddMinutes(15)));
    }

    [Fact]
    public void Sucesso_zera_tentativas_e_bloqueio()
    {
        var u = Novo();
        for (var i = 0; i < 5; i++) u.RegistrarFalha(Agora, 5, TimeSpan.FromMinutes(15));

        u.RegistrarSucesso(Agora);

        Assert.Equal(0, u.TentativasFalhas);
        Assert.False(u.EstaBloqueado(Agora));
    }

    [Fact]
    public void Desativado_ou_excluido_nao_pode_entrar()
    {
        var u = Novo();
        Assert.True(u.PodeEntrar);

        u.DefinirAtivo(false, Agora);
        Assert.False(u.PodeEntrar);

        u.DefinirAtivo(true, Agora);
        u.SystemDeleted = Guid.NewGuid();
        Assert.False(u.PodeEntrar);
    }
}
