using CacheFinancas.Domain.Entities;

namespace CacheFinancas.Application.Abstractions;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default);
    Task<bool> ExisteAlgumAsync(CancellationToken ct = default);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct = default);
    Task SalvarAsync(CancellationToken ct = default);
}
