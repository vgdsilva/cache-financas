using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Features.Users;
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.Tests.Infrastructure;
using VGS.FinanceIA.ViewComponents;

namespace VGS.FinanceIA.Tests.Framework;

public class LayoutRenderingTests
{
    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Name, "vinicius")],
                SchemeName);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed class FakeAllowedMenus : IQueryHandler<GetAllowedMenusQuery, IReadOnlyList<MenuEntry>>
    {
        public Task<IReadOnlyList<MenuEntry>> HandleAsync(GetAllowedMenusQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MenuEntry>>(Menus.All
                .Select(d => new MenuEntry(d.Code, d.Description, d.Icon, d.Url, d.SortOrder, d.ParentCode))
                .ToList());
    }

    private sealed class FakeProfile(bool isAdmin) : IQueryHandler<GetUserProfileQuery, UserProfile?>
    {
        public Task<UserProfile?> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<UserProfile?>(new UserProfile("vinicius", isAdmin));
    }

    private sealed class FakeHasAccess(bool canChat) : IQueryHandler<HasMenuAccessQuery, bool>
    {
        public Task<bool> HandleAsync(HasMenuAccessQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(query.MenuCode != Menus.Chat || canChat); // nega só o chat
    }

    /// <summary>App com usuário autenticado de mentira, menus fixos e perfil configurável, sem banco.</summary>
    private sealed class AuthenticatedFactory(bool isAdmin = true, bool canChat = true) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:InitializeOnStartup", "false");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.AddScoped<IQueryHandler<GetAllowedMenusQuery, IReadOnlyList<MenuEntry>>, FakeAllowedMenus>();
                services.AddScoped<IQueryHandler<GetUserProfileQuery, UserProfile?>>(_ => new FakeProfile(isAdmin));
                services.AddScoped<IQueryHandler<HasMenuAccessQuery, bool>>(_ => new FakeHasAccess(canChat));
            });
        }
    }

    private static async Task<string> GetHomeAsync(AuthenticatedFactory factory)
    {
        using var client = factory.CreateClient();

        return System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LoginPage_UsesAuthLayout_WithoutNavigation()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Login");

        Assert.Contains("data-login-form", html);
        Assert.Contains("name=\"UserName\"", html);
        Assert.Contains("type=\"password\"", html);
        Assert.Contains("data-password-toggle", html);
        Assert.DoesNotContain("data-app-shell", html);
        Assert.DoesNotContain("data-nav-toggle", html);
    }

    [Fact]
    public async Task Shell_HasSidebarWithTitledSections_AndHeader()
    {
        using var factory = new AuthenticatedFactory();

        var html = await GetHomeAsync(factory);

        Assert.Contains("data-app-shell", html);
        Assert.Contains("class=\"sidebar\"", html);
        Assert.Contains("class=\"app-header\"", html);
        Assert.Contains("data-nav-toggle", html);   // hambúrguer do header (mobile)
        Assert.Contains("data-nav-close", html);    // fechar a gaveta
        Assert.Contains("data-nav-collapse", html); // recolher no desktop
        Assert.Contains("aria-controls=\"app-menu\"", html);
        Assert.Contains($">{AppNavViewComponent.MainSectionTitle}<", html);
        Assert.Contains(">Configurações<", html); // título da seção do grupo
        Assert.Contains("href=\"/cadastros/lancamentos\"", html);
        Assert.Contains("href=\"/dashboard\"", html);
        Assert.Contains("href=\"/minha-carteira\"", html);
        Assert.Contains("href=\"/cacau-ia\"", html);
        Assert.Contains("href=\"/cadastros/contas\"", html);

        // Ordem das seções na sidebar: Principal, Cadastros, Configurações.
        var principal = html.IndexOf($">{AppNavViewComponent.MainSectionTitle}<", StringComparison.Ordinal);
        var cadastros = html.IndexOf(">Cadastros<", StringComparison.Ordinal);
        var configuracoes = html.IndexOf(">Configurações<", StringComparison.Ordinal);
        Assert.True(principal >= 0 && principal < cadastros && cadastros < configuracoes, "Seções fora de ordem.");
        Assert.Contains("href=\"/configuracoes/usuarios\"", html);
        Assert.DoesNotContain("href=\"/CONFIG\"", html);
    }

    [Fact]
    public async Task Header_ShowsUserMenu_WithRoleAndLogout()
    {
        using var factory = new AuthenticatedFactory(isAdmin: true);

        var html = await GetHomeAsync(factory);

        Assert.Contains("data-user-menu", html);
        Assert.Contains("vinicius", html);
        Assert.Contains("Administrador", html);
        Assert.Contains("action=\"/Logout\"", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task Header_ShowsCommonUserRole_ForNonAdmin()
    {
        using var factory = new AuthenticatedFactory(isAdmin: false);

        var html = await GetHomeAsync(factory);

        Assert.Contains(">Usuário<", html);
        Assert.DoesNotContain("Administrador", html);
    }

    [Fact]
    public async Task Header_ChatShortcut_OnlyWhenUserHasChatMenu()
    {
        using var withChat = new AuthenticatedFactory(canChat: true);
        using var withoutChat = new AuthenticatedFactory(canChat: false);

        Assert.Contains("aria-label=\"Cacau IA\"", await GetHomeAsync(withChat));
        Assert.DoesNotContain("aria-label=\"Cacau IA\"", await GetHomeAsync(withoutChat));
    }

    [Fact]
    public async Task Header_HasNoThemePicker_BecauseTheAppIsLightOnly()
    {
        using var factory = new AuthenticatedFactory();

        var html = await GetHomeAsync(factory);

        Assert.DoesNotContain("data-theme-option", html);
    }

    [Fact]
    public async Task EveryPage_AppliesCollapsedSidebarBeforeFirstPaint()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var login = await client.GetStringAsync("/Login");

        // O script inline do <head> precisa vir antes do CSS, para a página não piscar no estado errado.
        var script = login.IndexOf("localStorage.getItem(\"nav-collapsed\")", StringComparison.Ordinal);
        var stylesheet = login.IndexOf("rel=\"stylesheet\"", StringComparison.Ordinal);

        Assert.True(script >= 0, "Script do estado da sidebar ausente no <head>.");
        Assert.True(script < stylesheet, "O script do estado da sidebar deve vir antes do CSS.");
    }

    [Fact]
    public async Task Css_IsLightOnly()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var css = await client.GetStringAsync("/css/site.css");

        Assert.Contains("color-scheme: light;", css);
        Assert.DoesNotContain("light-dark(", css);
        Assert.DoesNotContain("data-theme", css);
        Assert.DoesNotContain("prefers-color-scheme", css);
    }
}
