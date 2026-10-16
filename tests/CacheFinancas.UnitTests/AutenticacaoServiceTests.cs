using CacheFinancas.Application.Abstractions;
using CacheFinancas.Application.Autenticacao;
using CacheFinancas.Domain.Entities;
using CacheFinancas.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheFinancas.UnitTests;

public class AutenticacaoServiceTests
{
    private sealed class FakeHasher : IPasswordHasher
    {
        public int Simulacoes { get; private set; }
        public string Hash(string senha) => "h:" + senha;
        public bool Verificar(string senha, string hash) => hash == "h:" + senha;
        public void SimularVerificacao(string senha) => Simulacoes++;
    }

    private sealed class FakeRepo : IUsuarioRepository
    {
        public List<Usuario> Itens { get; } = [];
        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Itens.FirstOrDefault(u => u.Id == id));
        public Task<Usuario?> ObterPorEmailAsync(string e, CancellationToken ct = default) =>
            Task.FromResult(Itens.FirstOrDefault(u => u.Email == e));
        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Itens);
        public Task<bool> ExisteAlgumAsync(CancellationToken ct = default) => Task.FromResult(Itens.Count > 0);
        public Task AdicionarAsync(Usuario u, CancellationToken ct = default) { Itens.Add(u); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = agora;
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private readonly FakeRepo _repo = new();
    private readonly FakeHasher _hasher = new();
    private readonly RelogioFalso _relogio = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    private AutenticacaoService Criar() =>
        new(_repo, _hasher, new LockoutOptions(), _relogio, NullLogger<AutenticacaoService>.Instance);

    private Usuario AdicionarUsuario(bool deveTrocar = false)
    {
        var u = Usuario.Criar("Ana", "ana@x.com", "h:senha-correta", PerfilUsuario.Usuario, deveTrocar);
        _repo.Itens.Add(u);
        return u;
    }

    [Fact]
    public async Task Login_com_sucesso_retorna_usuario()
    {
        AdicionarUsuario(deveTrocar: true);

        var r = await Criar().LoginAsync("ANA@x.com", "senha-correta");

        Assert.Equal(StatusLogin.Sucesso, r.Status);
        Assert.True(r.Usuario!.DeveTrocarSenha);
    }

    [Fact]
    public async Task Email_inexistente_e_senha_errada_retornam_mesmo_status()
    {
        AdicionarUsuario();

        var inexistente = await Criar().LoginAsync("nao@x.com", "qualquer");
        var errada = await Criar().LoginAsync("ana@x.com", "errada");

        Assert.Equal(StatusLogin.CredenciaisInvalidas, inexistente.Status);
        Assert.Equal(inexistente.Status, errada.Status);
        Assert.Equal(1, _hasher.Simulacoes);
    }

    [Fact]
    public async Task Usuario_inativo_nao_entra()
    {
        var u = AdicionarUsuario();
        u.DefinirAtivo(false, _relogio.GetUtcNow().UtcDateTime);

        var r = await Criar().LoginAsync("ana@x.com", "senha-correta");

        Assert.Equal(StatusLogin.CredenciaisInvalidas, r.Status);
    }

    [Fact]
    public async Task Cinco_falhas_bloqueiam_e_senha_correta_e_recusada_durante_o_bloqueio()
    {
        AdicionarUsuario();
        var svc = Criar();

        for (var i = 0; i < 5; i++) await svc.LoginAsync("ana@x.com", "errada");

        var r = await svc.LoginAsync("ana@x.com", "senha-correta");
        Assert.Equal(StatusLogin.Bloqueado, r.Status);

        _relogio.Agora = _relogio.Agora.AddMinutes(16);
        var depois = await svc.LoginAsync("ana@x.com", "senha-correta");
        Assert.Equal(StatusLogin.Sucesso, depois.Status);
    }

    [Fact]
    public async Task TrocarSenha_exige_senha_atual_e_tamanho_minimo()
    {
        var u = AdicionarUsuario(deveTrocar: true);
        var svc = Criar();

        Assert.False((await svc.TrocarSenhaAsync(u.Id, "errada", "nova-senha-longa")).Sucesso);
        Assert.False((await svc.TrocarSenhaAsync(u.Id, "senha-correta", "curta")).Sucesso);

        var ok = await svc.TrocarSenhaAsync(u.Id, "senha-correta", "nova-senha-longa");
        Assert.True(ok.Sucesso);
        Assert.False(u.DeveTrocarSenha);
        Assert.Equal("h:nova-senha-longa", u.SenhaHash);
    }
}
