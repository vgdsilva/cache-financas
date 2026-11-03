using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary><paramref name="MenuCount"/> é o total de menus concedidos (administrador acessa todos, independente disso).</summary>
public sealed record UserListItem(Guid Id, string UserName, string? FullName, string Email, bool IsAdmin, int MenuCount, DateTime CreatedAt)
{
    /// <summary>Nome completo, ou o login quando não há.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? UserName : FullName;
}

/// <summary>Usuários ativos em ordem alfabética, com busca opcional por parte do nome.</summary>
public sealed record ListUsersQuery(string? Search = null) : IQuery<IReadOnlyList<UserListItem>>;

public sealed class ListUsersHandler(AppDbContext db) : IQueryHandler<ListUsersQuery, IReadOnlyList<UserListItem>>
{
    public async Task<IReadOnlyList<UserListItem>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // % e _ digitados pelo usuário valem como texto, não como curinga.
            var escaped = query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";

            users = users.Where(u => EF.Functions.ILike(u.UserName, pattern, "\\"));
        }

        return await users
            .OrderBy(u => u.UserName)
            .Select(u => new UserListItem(
                u.Id,
                u.UserName,
                u.FullName,
                u.Email,
                u.IsAdmin,
                db.UserMenuPermissions.Count(p => p.UserId == u.Id),
                u.CreateAt))
            .ToListAsync(cancellationToken);
    }
}
