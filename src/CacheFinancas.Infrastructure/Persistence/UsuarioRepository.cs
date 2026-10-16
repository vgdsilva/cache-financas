using CacheFinancas.Application.Abstractions;
using CacheFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CacheFinancas.Infrastructure.Persistence;

public class UsuarioRepository(AppDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado, ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync(ct);

    public Task<bool> ExisteAlgumAsync(CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(ct);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken ct = default) =>
        await db.Usuarios.AddAsync(usuario, ct);

    public Task SalvarAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
