using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>
/// Cria um usuário e devolve o seu id. Não checa quem está pedindo (é o bloco básico, usado também pelo seed
/// do administrador inicial); a tela usa <see cref="RegisterUserCommand"/>, que exige um administrador.
/// </summary>
public sealed record CreateUserCommand(string UserName, string Password, bool IsAdmin, string Email, string? FullName = null)
    : ICommand<Guid>;

public sealed class CreateUserHandler(AppDbContext db, IPasswordHasher<User> hasher, IConfiguration configuration)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var userName = UserRules.NormalizeUserNameOrThrow(command.UserName);
        var email = UserRules.NormalizeEmailOrThrow(command.Email);
        var fullName = UserRules.NormalizeFullNameOrThrow(command.FullName);
        PasswordRules.Validate(configuration, command.Password);

        if (await db.Users.AnyAsync(u => u.UserName == userName, cancellationToken))
        {
            throw new BusinessRuleException($"O usuário '{userName}' já existe.", "UserName");
        }

        var user = new User { UserName = userName, Email = email, FullName = fullName, IsAdmin = command.IsAdmin };
        user.PasswordHash = hasher.HashPassword(user, command.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
