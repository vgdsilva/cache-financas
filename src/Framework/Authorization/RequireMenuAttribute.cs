using Microsoft.AspNetCore.Authorization;

namespace VGS.FinanceIA.Framework.Authorization;

/// <summary>
/// Exige acesso a um menu. Uso numa página: <c>[RequireMenu(Menus.Transactions)]</c>.
/// Cobre também os handlers HTMX da mesma página.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireMenuAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "menu:";

    public RequireMenuAttribute(string menuCode)
    {
        MenuCode = menuCode;
        Policy = PolicyPrefix + menuCode;
    }

    public string MenuCode { get; }
}
