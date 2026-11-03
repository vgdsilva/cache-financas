using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>
/// Exclui (logicamente) um usuário, junto com os menus e as configurações dele. Exige administrador;
/// ninguém exclui a si mesmo e o último administrador não pode ser excluído.
/// </summary>
public sealed record DeleteUserCommand(Guid ActingUserId, Guid UserId) : ICommand;

public sealed class DeleteUserHandler(AppDbContext db, IMemoryCache cache) : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        await UserRules.RequireAdminAsync(db, command.ActingUserId, cancellationToken);

        if (command.ActingUserId == command.UserId)
        {
            throw new BusinessRuleException("Você não pode excluir o seu próprio usuário.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new BusinessRuleException("Usuário não encontrado.");

        if (user.IsAdmin)
        {
            await UserRules.EnsureAnotherAdminAsync(db, user.Id, cancellationToken);
        }

        db.UserMenuPermissions.RemoveRange(await db.UserMenuPermissions.Where(p => p.UserId == user.Id).ToListAsync(cancellationToken));
        db.Configurations.RemoveRange(await db.Configurations.Where(c => c.UserId == user.Id).ToListAsync(cancellationToken));
        db.Users.Remove(user);

        await db.SaveChangesAsync(cancellationToken);

        cache.Remove(PermissionCache.Key(user.Id));
    }
}
