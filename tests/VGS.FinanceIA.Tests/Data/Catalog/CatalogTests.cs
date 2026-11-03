using VGS.FinanceIA.Data.Catalog;

namespace VGS.FinanceIA.Tests.Data.Catalog;

public class CatalogTests
{
    [Fact]
    public void Menus_CodesAreUnique()
    {
        var codes = Menus.All.Select(m => m.Code).ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void Menus_ParentsExistAndComeBeforeChildren()
    {
        var seen = new HashSet<string>();

        foreach (var menu in Menus.All)
        {
            if (menu.ParentCode is not null)
            {
                Assert.Contains(menu.ParentCode, seen);
            }

            seen.Add(menu.Code);
        }
    }

    [Fact]
    public void Menus_UrlsAreLocalPaths()
    {
        foreach (var menu in Menus.All.Where(m => m.Url is not null))
        {
            Assert.StartsWith("/", menu.Url);
        }
    }

    [Fact]
    public void Menus_UrlsAreLowercaseWithoutTrailingSlash_AndUnique()
    {
        var urls = Menus.All.Where(m => m.Url is not null).Select(m => m.Url!).ToList();

        Assert.Equal(urls.Count, urls.Distinct().Count());
        Assert.All(urls, url =>
        {
            Assert.Equal(url.ToLowerInvariant(), url);
            Assert.False(url.Length > 1 && url.EndsWith('/'), $"'{url}' termina com '/'.");
            Assert.Matches("^(/[a-z0-9-]+)+$", url);
        });
    }

    [Fact]
    public void Menus_ChildUrlsLiveUnderTheirParentGroup()
    {
        // O pai é só agrupador (sem URL): todos os filhos dividem o primeiro segmento da rota,
        // e esse segmento não pode ser usado por nenhuma página de outro menu.
        static string FirstSegment(string url) => url.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];

        var pages = Menus.All.Where(m => m.Url is not null).ToList();

        foreach (var group in pages.Where(m => m.ParentCode is not null).GroupBy(m => m.ParentCode))
        {
            var segments = group.Select(m => FirstSegment(m.Url!)).Distinct().ToList();
            Assert.True(segments.Count == 1, $"Filhos de '{group.Key}' em segmentos diferentes: {string.Join(", ", segments)}.");

            foreach (var page in group)
            {
                Assert.True(page.Url!.Count(c => c == '/') >= 2, $"'{page.Url}' deveria estar abaixo do grupo '{group.Key}'.");
            }

            var outsiders = pages.Where(m => m.ParentCode != group.Key && FirstSegment(m.Url!) == segments[0]);
            Assert.Empty(outsiders);
        }

        foreach (var topLevel in pages.Where(m => m.ParentCode is null))
        {
            Assert.Equal(1, topLevel.Url!.Count(c => c == '/'));
        }
    }

    [Fact]
    public void Menus_EveryGroupHasChildren_AndIsNotNavigable()
    {
        var groups = Menus.All.Where(m => m.Url is null).ToList();

        Assert.Equal(new[] { Menus.Registry, Menus.Settings }.Order(), groups.Select(g => g.Code).Order());
        Assert.All(groups, g => Assert.Contains(Menus.All, m => m.ParentCode == g.Code));
    }

    [Fact]
    public void Menus_Structure_MatchesTheSidebarDesign()
    {
        string[] Codes(string? parent) => Menus.All.Where(m => m.ParentCode == parent && m.Url is not null)
            .OrderBy(m => m.SortOrder).Select(m => m.Code).ToArray();

        Assert.Equal([Menus.Dashboard, Menus.Wallet, Menus.Chat], Codes(null));
        Assert.Equal([Menus.Accounts, Menus.Transactions, Menus.Categories], Codes(Menus.Registry));
        Assert.Equal([Menus.Users], Codes(Menus.Settings));
    }

    [Fact]
    public void Menus_UrlOf_FindsPageUrl_AndIgnoresGroupsAndUnknown()
    {
        Assert.Equal("/cacau-ia", Menus.UrlOf(Menus.Chat));
        Assert.Null(Menus.UrlOf(Menus.Registry));
        Assert.Null(Menus.UrlOf("NOPE"));
    }

    [Fact]
    public void ConfigurationKeys_AreUniqueAndHaveDefaults()
    {
        var keys = ConfigurationKeys.All.Select(c => c.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(ConfigurationKeys.All, c => Assert.False(string.IsNullOrWhiteSpace(c.DefaultValue)));
        Assert.Contains(ConfigurationKeys.AiModel, keys);
    }
}
