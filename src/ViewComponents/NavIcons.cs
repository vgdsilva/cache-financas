using Microsoft.AspNetCore.Html;

namespace VGS.FinanceIA.ViewComponents;

/// <summary>
/// Ícones sólidos (viewBox 20x20, <c>fill="currentColor"</c>) indexados pelo campo <c>Icon</c> do catálogo de menus.
/// Nome desconhecido cai no ícone padrão, então um menu novo nunca fica sem ícone.
/// </summary>
public static class NavIcons
{
    private const string Open =
        "<svg class=\"nav-icon\" viewBox=\"0 0 20 20\" fill=\"currentColor\" aria-hidden=\"true\">";

    private const string Close = "</svg>";

    private const string EvenOdd = "fill-rule=\"evenodd\" clip-rule=\"evenodd\"";

    private static readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chart"] = $"<path {EvenOdd} d=\"M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm3 11a1 1 0 11-2 0V9a1 1 0 112 0v5zm4 0a1 1 0 11-2 0V6a1 1 0 112 0v8zm4 0a1 1 0 11-2 0v-3a1 1 0 112 0v3z\"/>",
        ["bank"] = "<path d=\"M10 2L2 6v2h16V6l-8-4zM4 9h2v6H4V9zm5 0h2v6H9V9zm5 0h2v6h-2V9zM3 16h14v2H3v-2z\"/>",
        ["folder"] = "<path d=\"M2 6a2 2 0 012-2h4l2 2h6a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z\"/>",
        ["receipt"] = $"<path {EvenOdd} d=\"M5 2a1 1 0 00-1 1v15l2-1.5L8 18l2-1.5 2 1.5 2-1.5 2 1.5V3a1 1 0 00-1-1H5zM7 6h6v1.5H7V6zm0 3.5h6V11H7V9.5z\"/>",
        ["wallet"] = $"<path {EvenOdd} d=\"M3 6a2 2 0 012-2h10a1 1 0 010 2H5a.5.5 0 000 1h11a1 1 0 011 1v7a2 2 0 01-2 2H5a2 2 0 01-2-2V6zm11 5.5a1 1 0 100 2 1 1 0 000-2z\"/>",
        ["tag"] = $"<path {EvenOdd} d=\"M17.707 9.293a1 1 0 010 1.414l-7 7a1 1 0 01-1.414 0l-7-7A.997.997 0 012 10V5a3 3 0 013-3h5c.256 0 .512.098.707.293l7 7zM5 6a1 1 0 100-2 1 1 0 000 2z\"/>",
        ["chat"] = $"<path {EvenOdd} d=\"M18 10c0 3.866-3.582 7-8 7a8.84 8.84 0 01-2.83-.46L3 18l1.3-3.06A6.4 6.4 0 012 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm4 0H9v2h2V9zm2 0h2v2h-2V9z\"/>",
        ["gear"] = $"<path {EvenOdd} d=\"M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z\"/>",
        ["people"] = "<path d=\"M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z\"/>",
        ["logout"] = $"<path {EvenOdd} d=\"M3 4a1 1 0 011-1h6a1 1 0 110 2H5v10h5a1 1 0 110 2H4a1 1 0 01-1-1V4zm10.3 2.3a1 1 0 011.4 0l3 3a1 1 0 010 1.4l-3 3a1 1 0 01-1.4-1.4L14.58 11H8a1 1 0 110-2h6.58l-1.3-1.3a1 1 0 010-1.4z\"/>",
    };

    private const string Fallback = "<circle cx=\"10\" cy=\"10\" r=\"3\"/>";

    public static IHtmlContent Render(string? name)
    {
        var path = name is not null && _paths.TryGetValue(name, out var known) ? known : Fallback;

        return new HtmlString(Open + path + Close);
    }
}
