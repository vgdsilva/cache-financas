using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>Cria o administrador inicial apenas se ainda não existir nenhum usuário. Devolve true se criou.</summary>
public sealed record EnsureInitialAdminCommand(string UserName, string Password) : ICommand<bool>;

public sealed class EnsureInitialAdminHandler(AppDbContext db, ICommandHandler<CreateUserCommand, Guid> createUser)
    : ICommandHandler<EnsureInitialAdminCommand, bool>
{
    public async Task<bool> HandleAsync(EnsureInitialAdminCommand command, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        await createUser.HandleAsync(new CreateUserCommand(command.UserName, command.Password, IsAdmin: true), cancellationToken);

        return true;
    }
}
