using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VGS.FinanceIA.Data;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Features.Users;

/// <summary>Regras compartilhadas pelos commands de gestão de usuários.</summary>
internal static partial class UserRules
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 40;
    public const int EmailMaxLength = 254;
    public const int FullNameMaxLength = 120;

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex UserNamePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

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

    /// <summary>Login normalizado (minúsculas). Letras, dígitos, ponto, hífen e underscore; 3 a 40 caracteres.</summary>
    public static string NormalizeUserNameOrThrow(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new BusinessRuleException("Informe o usuário.", "UserName");
        }

        var normalized = UserNames.Normalize(userName);

        if (normalized.Length is < UserNameMinLength or > UserNameMaxLength || !UserNamePattern().IsMatch(normalized))
        {
            throw new BusinessRuleException(
                $"O usuário deve ter de {UserNameMinLength} a {UserNameMaxLength} caracteres e usar só letras, dígitos, ponto, hífen e underscore.",
                "UserName");
        }

        return normalized;
    }

    /// <summary>E-mail obrigatório, em minúsculas.</summary>
    public static string NormalizeEmailOrThrow(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessRuleException("Informe o e-mail.", "Email");
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > EmailMaxLength || !EmailPattern().IsMatch(normalized))
        {
            throw new BusinessRuleException("Informe um e-mail válido.", "Email");
        }

        return normalized;
    }

    /// <summary>Nome completo opcional: vazio vira nulo.</summary>
    public static string? NormalizeFullNameOrThrow(string? fullName)
    {
        var trimmed = fullName?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > FullNameMaxLength)
        {
            throw new BusinessRuleException($"O nome completo deve ter no máximo {FullNameMaxLength} caracteres.", "FullName");
        }

        return trimmed;
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
            throw new BusinessRuleException($"A senha deve ter pelo menos {minLength} caracteres.", "Password");
        }
    }
}
