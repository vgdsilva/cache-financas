using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.ViewComponents;

public sealed record AppHeaderModel(string UserName, string RoleLabel, bool CanChat, string? ChatUrl)
{
    public string Initial => UserName.Length > 0 ? UserName[..1].ToUpperInvariant() : "?";
}

/// <summary>Barra do topo: botão do menu (mobile), atalho do chat (se liberado) e menu do usuário.</summary>
public class AppHeaderViewComponent(
    IQueryHandler<GetUserProfileQuery, UserProfile?> profile,
    IQueryHandler<HasMenuAccessQuery, bool> hasAccess) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = CurrentUser.GetId(HttpContext.User);

        if (userId is null)
        {
            return Content(string.Empty);
        }

        var user = await profile.HandleAsync(new GetUserProfileQuery(userId.Value));
        var canChat = await hasAccess.HandleAsync(new HasMenuAccessQuery(userId.Value, Menus.Chat));

        // Sem perfil (usuário removido com cookie ainda válido): cai no nome do cookie e perfil comum.
        var model = new AppHeaderModel(
            user?.DisplayName ?? HttpContext.User.Identity?.Name ?? string.Empty,
            (user ?? new UserProfile(string.Empty, false)).RoleLabel,
            canChat,
            Menus.UrlOf(Menus.Chat));

        return View(model);
    }
}
