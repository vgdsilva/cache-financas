using System.Security.Claims;

namespace VGS.FinanceIA.Framework.Authorization;

public static class CurrentUser
{
    public static Guid? GetId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
