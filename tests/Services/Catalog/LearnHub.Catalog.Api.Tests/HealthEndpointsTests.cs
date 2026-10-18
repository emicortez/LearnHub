using System.Net;

namespace LearnHub.Catalog.Api.Tests;

public sealed class HealthEndpointsTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
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
