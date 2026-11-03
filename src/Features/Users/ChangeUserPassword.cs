using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>Redefine a senha de um usuário. Exige administrador. As sessões já abertas continuam valendo até expirar.</summary>
public sealed record ChangeUserPasswordCommand(Guid ActingUserId, Guid UserId, string NewPassword) : ICommand;

public sealed class ChangeUserPasswordHandler(AppDbContext db, IPasswordHasher<User> hasher, IConfiguration configuration)
    : ICommandHandler<ChangeUserPasswordCommand>
{
    public async Task HandleAsync(ChangeUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        await UserRules.RequireAdminAsync(db, command.ActingUserId, cancellationToken);
        PasswordRules.Validate(configuration, command.NewPassword);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            ?? throw new BusinessRuleException("Usuário não encontrado.");

        user.PasswordHash = hasher.HashPassword(user, command.NewPassword);

        await db.SaveChangesAsync(cancellationToken);
    }
}
