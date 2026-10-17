using CacheFinancas.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace CacheFinancas.Server.Modules.Usuarios;

public class UsuarioService(AppDbContext db)
{
    public async Task<List<UsuarioResponse>> ListarAsync(CancellationToken ct)
        => await db.Set<Usuario>().AsNoTracking()
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioResponse(u.Id, u.Nome, u.Email, u.CriadoEm))
            .ToListAsync(ct);

    public async Task<UsuarioResponse?> ObterAsync(Guid id, CancellationToken ct)
        => await db.Set<Usuario>().AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioResponse(u.Id, u.Nome, u.Email, u.CriadoEm))
            .FirstOrDefaultAsync(ct);

    /// <returns>null quando o e-mail ja existe.</returns>
    public async Task<UsuarioResponse?> CriarAsync(CriarUsuarioRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Set<Usuario>().AnyAsync(u => u.Email == email, ct))
            return null;

        var usuario = new Usuario(req.Nome.Trim(), email);
        db.Add(usuario);
        await db.SaveChangesAsync(ct);
        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
    }
}
