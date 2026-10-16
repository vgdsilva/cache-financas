using CacheFinancas.Application.Abstractions;
using CacheFinancas.Domain.Entities;
using CacheFinancas.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CacheFinancas.Application.Autenticacao;

/// <summary>Cria o admin inicial quando não existe nenhum usuário. Depois disso, as variáveis são ignoradas.</summary>
public class AdminBootstrapper(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    BootstrapOptions opcoes,
    ILogger<AdminBootstrapper> logger)
{
    public async Task ExecutarAsync(CancellationToken ct = default)
    {
        if (await usuarios.ExisteAlgumAsync(ct)) return;

        if (string.IsNullOrWhiteSpace(opcoes.AdminEmail) || string.IsNullOrEmpty(opcoes.AdminSenha))
        {
            logger.LogWarning("Nenhum usuário cadastrado e Bootstrap__AdminEmail/Bootstrap__AdminSenha não definidos; ninguém conseguirá entrar.");
            return;
        }

        if (opcoes.AdminSenha.Length < Usuario.TamanhoMinimoSenha)
        {
            logger.LogError("Bootstrap__AdminSenha deve ter ao menos {Min} caracteres; admin inicial não criado.", Usuario.TamanhoMinimoSenha);
            return;
        }

        var admin = Usuario.Criar("Administrador", opcoes.AdminEmail, hasher.Hash(opcoes.AdminSenha), PerfilUsuario.Admin, deveTrocarSenha: true);
        await usuarios.AdicionarAsync(admin, ct);
        await usuarios.SalvarAsync(ct);
        logger.LogInformation("Admin inicial criado ({Email})", admin.Email);
    }
}
