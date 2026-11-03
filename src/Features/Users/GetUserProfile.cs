using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>Dados de exibição do usuário (nome e perfil). Só leitura; não serve para decidir acesso.</summary>
public sealed record UserProfile(string UserName, bool IsAdmin)
{
    public string RoleLabel => IsAdmin ? "Administrador" : "Usuário";
}

/// <summary>Perfil do usuário, ou nulo se ele não existir (ou tiver sido excluído).</summary>
public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfile?>;

public sealed class GetUserProfileHandler(AppDbContext db) : IQueryHandler<GetUserProfileQuery, UserProfile?>
{
    public async Task<UserProfile?> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
    {
        // O filtro global de exclusão lógica já esconde usuário removido.
        return await db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserProfile(u.UserName, u.IsAdmin))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
