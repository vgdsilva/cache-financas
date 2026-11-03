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
using VGS.FinanceIA.Framework.Cqrs;
using VGS.FinanceIA.Tests.Infrastructure;

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

    /// <summary>App com usuário autenticado de mentira e menus fixos, sem banco.</summary>
    private sealed class AuthenticatedFactory : WebApplicationFactory<Program>
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
            });
        }
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
    public async Task Shell_RendersTopbarToggler_AndMenusFromHandler()
    {
        using var factory = new AuthenticatedFactory();
        using var client = factory.CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("data-app-shell", html);
        Assert.Contains("data-nav-toggle", html);
        Assert.Contains("aria-controls=\"app-menu\"", html);
        Assert.Contains("href=\"/Transactions\"", html);
        Assert.Contains("Lançamentos", html);
        Assert.Contains("Configurações", html); // título da seção do grupo
        Assert.Contains("href=\"/Users\"", html);
        Assert.DoesNotContain("href=\"/CONFIG\"", html);
    }

    [Fact]
    public async Task Shell_ShowsUserAndLogoutForm()
    {
        using var factory = new AuthenticatedFactory();
        using var client = factory.CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("vinicius", html);
        Assert.Contains("action=\"/Logout\"", html);
        Assert.Contains("__RequestVerificationToken", html);
    }
}
