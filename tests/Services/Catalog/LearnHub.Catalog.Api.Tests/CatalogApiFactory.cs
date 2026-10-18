using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// The shared host for API tests. Every TestServer request has no remote IP, so all tests share one
/// write rate-limit partition; the limit is raised far above what any test sends so they are never throttled.
/// Rate-limit tests lower it again with <c>WithWebHostBuilder</c>.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("RateLimiting:Writes:TokenLimit", "100000");
        builder.UseSetting("RateLimiting:Writes:TokensPerPeriod", "100000");
    }
}
