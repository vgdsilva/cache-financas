using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VGS.FinanceIA.Data.Catalog;
using VGS.FinanceIA.Features.Permissions;
using VGS.FinanceIA.Framework.Authorization;
using VGS.FinanceIA.Framework.Cqrs;

namespace VGS.FinanceIA.Tests.Framework.Authorization;

public class MenuAuthorizationTests
{
    private sealed class FakeHasAccess(Guid allowedUser, params string[] allowedCodes) : IQueryHandler<HasMenuAccessQuery, bool>
    {
        public Task<bool> HandleAsync(HasMenuAccessQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(query.UserId == allowedUser && allowedCodes.Contains(query.MenuCode));
    }

    private static ClaimsPrincipal PrincipalFor(string? userId)
    {
        var claims = userId is null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static async Task<bool> RunAsync(FakeHasAccess hasAccess, ClaimsPrincipal user, string menuCode)
    {
        var requirement = new MenuRequirement(menuCode);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new MenuAuthorizationHandler(hasAccess).HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task Handler_Succeeds_WhenUserHasMenu()
    {
        var id = Guid.NewGuid();

        Assert.True(await RunAsync(new FakeHasAccess(id, Menus.Chat), PrincipalFor(id.ToString()), Menus.Chat));
    }

    [Fact]
    public async Task Handler_Fails_WhenUserLacksMenu()
    {
        var id = Guid.NewGuid();

        Assert.False(await RunAsync(new FakeHasAccess(id, Menus.Chat), PrincipalFor(id.ToString()), Menus.Users));
    }

    [Fact]
    public async Task Handler_Fails_ForAnotherUser()
    {
        var hasAccess = new FakeHasAccess(Guid.NewGuid(), Menus.Chat);

        Assert.False(await RunAsync(hasAccess, PrincipalFor(Guid.NewGuid().ToString()), Menus.Chat));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Handler_Fails_WithoutValidUserIdClaim(string? userId)
    {
        var hasAccess = new FakeHasAccess(Guid.NewGuid(), Menus.Chat);

        Assert.False(await RunAsync(hasAccess, PrincipalFor(userId), Menus.Chat));
    }

    [Fact]
    public async Task PolicyProvider_BuildsMenuPolicy_OnDemand()
    {
        var provider = new MenuPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("menu:CONFIG.USERS");

        Assert.NotNull(policy);
        Assert.Equal("CONFIG.USERS", policy.Requirements.OfType<MenuRequirement>().Single().MenuCode);
    }

    [Fact]
    public async Task PolicyProvider_IgnoresOtherPolicyNames()
    {
        var provider = new MenuPolicyProvider(Options.Create(new AuthorizationOptions()));

        Assert.Null(await provider.GetPolicyAsync("something-else"));
    }

    [Fact]
    public void RequireMenuAttribute_UsesPolicyPrefix()
    {
        var attribute = new RequireMenuAttribute(Menus.Transactions);

        Assert.Equal("menu:REG.TRANSACTIONS", attribute.Policy);
    }
}
