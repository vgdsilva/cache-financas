using VGS.FinanceIA.Data;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>
/// Cadastro de usuário pela tela: exige que quem pede seja administrador, cria o usuário e já concede os menus.
/// Administrador acessa tudo, então os menus só valem para usuário comum.
/// </summary>
public sealed record RegisterUserCommand(
    Guid ActingUserId,
    string UserName,
    string Email,
    string? FullName,
    string Password,
    bool IsAdmin,
    IReadOnlyCollection<string> MenuCodes) : ICommand<Guid>;

public sealed class RegisterUserHandler(
    AppDbContext db,
    ICommandHandler<CreateUserCommand, Guid> createUser,
    ICommandHandler<SetUserMenusCommand> setMenus) : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        await UserRules.RequireAdminAsync(db, command.ActingUserId, cancellationToken);

        // Usuário e menus na mesma transação: se um menu for inválido, o usuário não fica criado pela metade.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var userId = await createUser.HandleAsync(
            new CreateUserCommand(command.UserName, command.Password, command.IsAdmin, command.Email, command.FullName),
            cancellationToken);

        if (!command.IsAdmin)
        {
            await setMenus.HandleAsync(new SetUserMenusCommand(userId, command.MenuCodes), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return userId;
    }
}
