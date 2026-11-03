using Microsoft.AspNetCore.Authorization;

namespace VGS.FinanceIA.Framework.Authorization;

public sealed class MenuRequirement(string menuCode) : IAuthorizationRequirement
{
    public string MenuCode { get; } = menuCode;
}
