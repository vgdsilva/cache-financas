using Microsoft.AspNetCore.Authorization;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Framework.Authorization;

/// <summary>Decide a policy "menu:CODIGO" perguntando à mesma query que as tools de IA usam.</summary>
public sealed class MenuAuthorizationHandler(IQueryHandler<HasMenuAccessQuery, bool> hasAccess)
    : AuthorizationHandler<MenuRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MenuRequirement requirement)
    {
        var userId = CurrentUser.GetId(context.User);

        if (userId is not null && await hasAccess.HandleAsync(new HasMenuAccessQuery(userId.Value, requirement.MenuCode)))
        {
            context.Succeed(requirement);
        }
    }
}
