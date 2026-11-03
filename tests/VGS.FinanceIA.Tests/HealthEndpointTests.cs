using System.Net;
using VGS.FinanceIA.Tests.Infrastructure;

namespace VGS.FinanceIA.Tests;

public class HealthEndpointTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Health_ReturnsOk_WithoutAuthentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
