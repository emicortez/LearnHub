using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnHub.Catalog.Api.Tests;

public sealed class HealthEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_endpoints_report_healthy(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
