using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.ViewComponents;

namespace VGS.FinanceIA.Tests.ViewComponents;

public class AppNavSectionsTests
{
    private static MenuEntry Entry(MenuDefinition definition)
        => new(definition.Code, definition.Description, definition.Icon, definition.Url, definition.SortOrder, definition.ParentCode);

    private static IReadOnlyList<MenuEntry> All() => Menus.All.Select(Entry).ToList();

    [Fact]
    public void Admin_GetsTopSection_AndSettingsGroupWithChildren()
    {
        var sections = AppNavViewComponent.BuildSections(All());

        Assert.Equal(2, sections.Count);
        Assert.Null(sections[0].Title);
        Assert.Equal([Menus.Transactions, Menus.Accounts, Menus.Categories, Menus.Chat], sections[0].Items.Select(i => i.Code));
        Assert.Equal("Configurações", sections[1].Title);
        Assert.Equal([Menus.Users], sections[1].Items.Select(i => i.Code));
    }

    [Fact]
    public void GroupHeader_IsNeverRenderedAsALink()
    {
        var sections = AppNavViewComponent.BuildSections(All());

        Assert.DoesNotContain(sections.SelectMany(s => s.Items), i => i.Code == Menus.Settings);
    }

    [Fact]
    public void GroupWithoutAllowedChildren_IsOmitted()
    {
        var menus = All().Where(m => m.Code is Menus.Chat or Menus.Settings).ToList();

        var sections = AppNavViewComponent.BuildSections(menus);

        Assert.Single(sections);
        Assert.Equal([Menus.Chat], sections[0].Items.Select(i => i.Code));
    }

    [Fact]
    public void Child_WhoseParentIsNotAllowed_FallsBackToTopSection()
    {
        var menus = All().Where(m => m.Code == Menus.Users).ToList();

        var sections = AppNavViewComponent.BuildSections(menus);

        Assert.Single(sections);
        Assert.Null(sections[0].Title);
        Assert.Equal([Menus.Users], sections[0].Items.Select(i => i.Code));
    }

    [Fact]
    public void NoMenus_NoSections() => Assert.Empty(AppNavViewComponent.BuildSections([]));

    [Theory]
    [InlineData("vinicius", "V")]
    [InlineData("ana", "A")]
    [InlineData("", "?")]
    public void Initial_IsFirstLetterUppercased(string name, string expected)
        => Assert.Equal(expected, new AppNavModel(name, []).Initial);

    [Fact]
    public void EveryCatalogIcon_HasAnSvg_AndUnknownFallsBack()
    {
        foreach (var definition in Menus.All.Where(d => d.Icon is not null))
        {
            var known = NavIcons.Render(definition.Icon).ToString();
            var fallback = NavIcons.Render("does-not-exist").ToString();

            Assert.Contains("<svg", known);
            Assert.NotEqual(fallback, known);
        }

        Assert.Contains("<svg", NavIcons.Render(null).ToString());
    }
}
