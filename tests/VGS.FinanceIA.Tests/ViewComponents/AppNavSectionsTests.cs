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
    public void Admin_GetsPrincipalCadastrosAndConfiguracoes_InOrder()
    {
        var sections = AppNavViewComponent.BuildSections(All());

        Assert.Equal([AppNavViewComponent.MainSectionTitle, "Cadastros", "Configurações"], sections.Select(s => s.Title));
        Assert.Equal([Menus.Dashboard, Menus.Wallet, Menus.Chat], sections[0].Items.Select(i => i.Code));
        Assert.Equal([Menus.Accounts, Menus.Transactions, Menus.Categories], sections[1].Items.Select(i => i.Code));
        Assert.Equal([Menus.Users], sections[2].Items.Select(i => i.Code));
    }

    [Fact]
    public void CacauIa_IsTheChatMenu_InPrincipal()
    {
        var chat = AppNavViewComponent.BuildSections(All())[0].Items.Single(i => i.Code == Menus.Chat);

        Assert.Equal("Cacau IA", chat.Description);
        Assert.Equal("/cacau-ia", chat.Url);
    }

    [Fact]
    public void GroupHeader_IsNeverRenderedAsALink()
    {
        var sections = AppNavViewComponent.BuildSections(All());

        Assert.DoesNotContain(sections.SelectMany(s => s.Items), i => i.Code is Menus.Settings or Menus.Registry);
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
        Assert.Equal(AppNavViewComponent.MainSectionTitle, sections[0].Title);
        Assert.Equal([Menus.Users], sections[0].Items.Select(i => i.Code));
    }

    [Fact]
    public void NoMenus_NoSections() => Assert.Empty(AppNavViewComponent.BuildSections([]));

    [Theory]
    [InlineData("vinicius", "V")]
    [InlineData("ana", "A")]
    [InlineData("", "?")]
    public void Initial_IsFirstLetterUppercased(string name, string expected)
        => Assert.Equal(expected, new AppHeaderModel(name, string.Empty, false, null).Initial);

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
