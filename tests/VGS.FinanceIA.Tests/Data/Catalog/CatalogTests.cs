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
    public void ConfigurationKeys_AreUniqueAndHaveDefaults()
    {
        var keys = ConfigurationKeys.All.Select(c => c.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(ConfigurationKeys.All, c => Assert.False(string.IsNullOrWhiteSpace(c.DefaultValue)));
        Assert.Contains(ConfigurationKeys.AiModel, keys);
    }
}
