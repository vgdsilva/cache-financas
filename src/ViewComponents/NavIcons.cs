using Microsoft.AspNetCore.Html;

namespace VGS.FinanceIA.ViewComponents;

/// <summary>
/// Ícones (SVG inline, traço) indexados pelo campo <c>Icon</c> do catálogo de menus.
/// Nome desconhecido cai no ícone padrão, então um menu novo nunca fica sem ícone.
/// </summary>
public static class NavIcons
{
    private const string Open =
        "<svg class=\"nav-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">";

    private const string Close = "</svg>";

    private static readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["receipt"] = "<path d=\"M6 3h12v18l-3-2-3 2-3-2-3 2z\"/><path d=\"M9 8h6M9 12h6\"/>",
        ["wallet"] = "<path d=\"M3 7h18v12H3z\"/><path d=\"M3 7l2-3h12v3\"/><circle cx=\"16.5\" cy=\"13\" r=\"1\"/>",
        ["tag"] = "<path d=\"M3 12V4h8l10 10-8 8z\"/><circle cx=\"7.5\" cy=\"8.5\" r=\"1\"/>",
        ["chat"] = "<path d=\"M4 5h16v11H9l-5 4z\"/>",
        ["gear"] = "<path d=\"M4 7h10M18 7h2M4 17h2M10 17h10\"/><circle cx=\"16\" cy=\"7\" r=\"2\"/><circle cx=\"8\" cy=\"17\" r=\"2\"/>",
        ["people"] = "<circle cx=\"9\" cy=\"8\" r=\"3\"/><path d=\"M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6\"/><circle cx=\"17\" cy=\"9\" r=\"2.5\"/><path d=\"M17 14c2.5 0 4 2 4 5\"/>",
        ["logout"] = "<path d=\"M10 4H5v16h5\"/><path d=\"M14 8l4 4-4 4M18 12H9\"/>",
    };

    private const string Fallback = "<circle cx=\"12\" cy=\"12\" r=\"3\"/>";

    public static IHtmlContent Render(string? name)
    {
        var path = name is not null && _paths.TryGetValue(name, out var known) ? known : Fallback;

        return new HtmlString(Open + path + Close);
    }
}
