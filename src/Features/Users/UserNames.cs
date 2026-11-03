namespace VGS.FinanceIA.Features.Users;

public static class UserNames
{
    /// <summary>Tamanho mínimo padrão da senha. Pode ser alterado por <c>Auth:MinPasswordLength</c> (use só em desenvolvimento).</summary>
    public const int DefaultMinPasswordLength = 8;

    public const string MinPasswordLengthSetting = "Auth:MinPasswordLength";

    /// <summary>Login sempre sem espaços nas pontas e em minúsculas.</summary>
    public static string Normalize(string userName) => userName.Trim().ToLowerInvariant();
}
