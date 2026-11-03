using Microsoft.AspNetCore.Mvc;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.ViewComponents;

/// <summary>Grupo de links da navegação. <paramref name="Title"/> nulo = grupo sem título (topo).</summary>
public sealed record NavSection(string? Title, IReadOnlyList<MenuEntry> Items);

public sealed record AppNavModel(string UserName, IReadOnlyList<NavSection> Sections)
{
    public string Initial => UserName.Length > 0 ? UserName[..1].ToUpperInvariant() : "?";
}

/// <summary>
/// Navegação principal (topbar com hambúrguer no mobile, sidebar no desktop).
/// Mostra apenas os menus liberados ao usuário logado.
/// </summary>
public class AppNavViewComponent(IQueryHandler<GetAllowedMenusQuery, IReadOnlyList<MenuEntry>> allowedMenus) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = CurrentUser.GetId(HttpContext.User);

        if (userId is null)
        {
            return Content(string.Empty);
        }

        var menus = await allowedMenus.HandleAsync(new GetAllowedMenusQuery(userId.Value));

        return View(new AppNavModel(HttpContext.User.Identity?.Name ?? string.Empty, BuildSections(menus)));
    }

    /// <summary>
    /// Menus sem URL são agrupadores (ex.: Configurações): viram o título de uma seção com os filhos liberados.
    /// Itens sem pai (ou cujo pai não está liberado ao usuário) ficam na seção sem título, no topo.
    /// </summary>
    internal static IReadOnlyList<NavSection> BuildSections(IReadOnlyList<MenuEntry> menus)
    {
        var headers = menus.Where(m => m.Url is null).ToList();
        var headerCodes = headers.Select(h => h.Code).ToHashSet();
        var links = menus.Where(m => m.Url is not null).ToList();

        var sections = new List<NavSection>();

        var top = links.Where(m => m.ParentCode is null || !headerCodes.Contains(m.ParentCode)).ToList();

        if (top.Count > 0)
        {
            sections.Add(new NavSection(null, top));
        }

        foreach (var header in headers.OrderBy(h => h.SortOrder))
        {
            var children = links.Where(m => m.ParentCode == header.Code).OrderBy(m => m.SortOrder).ToList();

            if (children.Count > 0)
            {
                sections.Add(new NavSection(header.Description, children));
            }
        }

        return sections;
    }
}
