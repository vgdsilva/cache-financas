using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>Regras compartilhadas pelos commands de gestão de usuários.</summary>
internal static class UserRules
{
    /// <summary>Só administradores gerenciam usuários (evita que um usuário comum crie admins ou conceda menus a mais).</summary>
    public static async Task RequireAdminAsync(AppDbContext db, Guid actingUserId, CancellationToken cancellationToken)
    {
        var isAdmin = await db.Users
            .Where(u => u.Id == actingUserId)
            .Select(u => u.IsAdmin)
            .FirstOrDefaultAsync(cancellationToken);

        if (!isAdmin)
        {
            throw new BusinessRuleException("Apenas administradores podem gerenciar usuários.");
        }
    }

    /// <summary>Garante que, sem o usuário informado, ainda sobra pelo menos um administrador.</summary>
    public static async Task EnsureAnotherAdminAsync(AppDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.IsAdmin && u.Id != userId, cancellationToken))
        {
            throw new BusinessRuleException("Deve existir pelo menos um administrador.");
        }
    }

    public static string NormalizeOrThrow(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new BusinessRuleException("Informe o usuário.");
        }

        return UserNames.Normalize(userName);
    }
}

/// <summary>Regra de tamanho mínimo da senha (padrão 8; <c>Auth:MinPasswordLength</c> só para desenvolvimento).</summary>
internal static class PasswordRules
{
    public static void Validate(IConfiguration configuration, string? password)
    {
        var minLength = Math.Max(1, configuration.GetValue(UserNames.MinPasswordLengthSetting, UserNames.DefaultMinPasswordLength));

        if (password is null || password.Length < minLength)
        {
            throw new BusinessRuleException($"A senha deve ter pelo menos {minLength} caracteres.");
        }
    }
}
