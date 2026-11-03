using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

public sealed record AuthenticatedUser(Guid Id, string UserName);

/// <summary>
/// Confere login e senha; devolve o usuário ou null. É um Command (e não Query) porque pode
/// regravar o hash quando o algoritmo do hasher evolui.
/// </summary>
public sealed record AuthenticateUserCommand(string UserName, string Password) : ICommand<AuthenticatedUser?>;

public sealed class AuthenticateUserHandler(AppDbContext db, IPasswordHasher<User> hasher)
    : ICommandHandler<AuthenticateUserCommand, AuthenticatedUser?>
{
    // Quando o usuário não existe, gasta o mesmo tempo de um hash (não revela logins válidos pelo tempo de resposta).
    private static readonly User _dummyUser = new() { UserName = "dummy" };

    public async Task<AuthenticatedUser?> HandleAsync(AuthenticateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserName) || string.IsNullOrEmpty(command.Password))
        {
            return null;
        }

        var userName = UserNames.Normalize(command.UserName);
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == userName, cancellationToken);

        if (user is null)
        {
            hasher.HashPassword(_dummyUser, command.Password);
            return null;
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, command.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new AuthenticatedUser(user.Id, user.UserName);
    }
}
