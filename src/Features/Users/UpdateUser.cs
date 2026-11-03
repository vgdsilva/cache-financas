using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>
/// Altera usuário, e-mail, nome completo, perfil e menus de um usuário. Exige administrador. Administrador acessa
/// tudo, então os menus só são aplicados para usuário comum (os já concedidos ficam guardados se ele virar administrador).
/// </summary>
public sealed record UpdateUserCommand(
    Guid ActingUserId,
    Guid UserId,
    string UserName,
    string Email,
    string? FullName,
    bool IsAdmin,
    IReadOnlyCollection<string> MenuCodes) : ICommand;

public sealed class UpdateUserHandler(
    AppDbContext db,
    IMemoryCache cache,
    ICommandHandler<SetUserMenusCommand> setMenus) : ICommandHandler<UpdateUserCommand>
{
    public async Task HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        await UserRules.RequireAdminAsync(db, command.ActingUserId, cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new BusinessRuleException("Usuário não encontrado.");

        var userName = UserRules.NormalizeUserNameOrThrow(command.UserName);
        var email = UserRules.NormalizeEmailOrThrow(command.Email);
        var fullName = UserRules.NormalizeFullNameOrThrow(command.FullName);

        if (await db.Users.AnyAsync(u => u.UserName == userName && u.Id != user.Id, cancellationToken))
        {
            throw new BusinessRuleException($"O usuário '{userName}' já existe.", "UserName");
        }

        if (user.IsAdmin && !command.IsAdmin)
        {
            await UserRules.EnsureAnotherAdminAsync(db, user.Id, cancellationToken);
        }

        user.UserName = userName;
        user.Email = email;
        user.FullName = fullName;
        user.IsAdmin = command.IsAdmin;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        if (!command.IsAdmin)
        {
            await setMenus.HandleAsync(new SetUserMenusCommand(user.Id, command.MenuCodes), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        // Virar (ou deixar de ser) administrador muda os menus visíveis na hora.
        cache.Remove(PermissionCache.Key(user.Id));
    }
}
