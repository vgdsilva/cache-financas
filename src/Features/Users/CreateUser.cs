using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Data.Types;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>Cria um usuário e devolve o seu id.</summary>
public sealed record CreateUserCommand(string UserName, string Password, bool IsAdmin) : ICommand<Guid>;

public sealed class CreateUserHandler(AppDbContext db, IPasswordHasher<User> hasher, IConfiguration configuration)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserName);

        var minLength = Math.Max(1, configuration.GetValue(UserNames.MinPasswordLengthSetting, UserNames.DefaultMinPasswordLength));

        if (command.Password is null || command.Password.Length < minLength)
        {
            throw new ArgumentException($"A senha deve ter pelo menos {minLength} caracteres.", nameof(command));
        }

        var userName = UserNames.Normalize(command.UserName);

        if (await db.Users.AnyAsync(u => u.UserName == userName, cancellationToken))
        {
            throw new InvalidOperationException($"O usuário '{userName}' já existe.");
        }

        var user = new User { UserName = userName, IsAdmin = command.IsAdmin };
        user.PasswordHash = hasher.HashPassword(user, command.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
