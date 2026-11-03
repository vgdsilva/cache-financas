using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests.Framework;

public class ProtectedPagesTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Anonymous_IsRedirectedToLogin_WithReturnUrl()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Login?ReturnUrl=%2F", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Anonymous_HtmxRequest_GetsUnauthorizedWithHxRedirect()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("HX-Request", "true");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("/Login", response.Headers.GetValues("HX-Redirect").Single());
    }

    [Fact]
    public async Task LoginPage_IsPublic()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessDeniedPage_IsPublic()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/AccessDenied");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StaticAssets_ArePublic()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
