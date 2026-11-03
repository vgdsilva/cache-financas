using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Framework;

/// <summary>
/// Convenção: a rota da página segue o caminho do menu (<c>/configuracoes/usuarios</c>, <c>.../create</c>, <c>.../edit/{id}</c>).
/// Vale para toda página que exige um menu; hoje ainda não existem páginas assim, então estes testes passam a
/// valer assim que as telas da etapa (c) entrarem.
/// </summary>
public class PageRoutesTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private const string PolicyPrefix = "menu:";

    private sealed record PageRoute(string Route, string? MenuCode);

    private List<PageRoute> PageRoutes() => factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
        .OfType<RouteEndpoint>()
        .Where(e => e.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor>() is not null)
        .Select(e => new PageRoute(
            "/" + e.RoutePattern.RawText!.TrimStart('/'),
            e.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Select(a => a.Policy)
                .FirstOrDefault(p => p?.StartsWith(PolicyPrefix, StringComparison.Ordinal) == true)?[PolicyPrefix.Length..]))
        .ToList();

    private static bool IsUnder(string route, string menuUrl)
        => route.Equals(menuUrl, StringComparison.OrdinalIgnoreCase)
           || route.StartsWith(menuUrl + "/", StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Discovery_SeesTheExistingPages()
    {
        // Garante que os dois testes abaixo não passam "no vazio" por falha na descoberta das rotas.
        var routes = PageRoutes().Select(p => p.Route).ToList();

        Assert.Contains("/Login", routes, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/AccessDenied", routes, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void PageRequiringAMenu_LivesUnderThatMenuUrl()
    {
        foreach (var page in PageRoutes().Where(p => p.MenuCode is not null))
        {
            var menu = Menus.All.Single(m => m.Code == page.MenuCode);

            Assert.NotNull(menu.Url);
            Assert.True(IsUnder(page.Route, menu.Url!), $"A página '{page.Route}' exige '{menu.Code}', cuja URL é '{menu.Url}'.");
        }
    }

    [Fact]
    public void PageUnderAMenuUrl_RequiresThatMenu()
    {
        var menusWithPages = Menus.All.Where(m => m.Url is not null).ToList();

        foreach (var page in PageRoutes())
        {
            var owner = menusWithPages.SingleOrDefault(m => IsUnder(page.Route, m.Url!));

            if (owner is not null)
            {
                Assert.True(page.MenuCode == owner.Code, $"A página '{page.Route}' está sob '{owner.Url}' mas não declara [RequireMenu(\"{owner.Code}\")].");
            }
        }
    }
}
