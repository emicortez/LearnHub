using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace LearnHub.Catalog.Api.Tests;

// Each test builds its own factory: the limiter's buckets live in the host, so a shared fixture
// would let one test's spent tokens throttle the next.
public sealed class RateLimitingTests
{
    [Fact]
    public async Task Create_course_beyond_the_write_limit_returns_429_with_problem_details_and_retry_after()
    {
        await using var factory = new CatalogApiFactory();
        var client = WithWriteLimit(factory, tokenLimit: 2).CreateClient();

        await CreateCourseAsync(client);
        await CreateCourseAsync(client);
        var response = await CreateCourseAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("catalog.request.rate_limited", problem.GetProperty("code").GetString());
        Assert.Equal("Too Many Requests", problem.GetProperty("title").GetString());
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        var retryAfter = Assert.Single(response.Headers.GetValues("Retry-After"));
        Assert.True(int.TryParse(retryAfter, out var seconds) && seconds > 0, $"Retry-After '{retryAfter}' is not a positive integer.");
    }

    [Fact]
    public async Task Publish_course_beyond_the_write_limit_returns_429()
    {
        await using var factory = new CatalogApiFactory();
        var client = WithWriteLimit(factory, tokenLimit: 1).CreateClient();

        await client.PostAsync($"/courses/{Guid.NewGuid()}/publish", content: null, TestContext.Current.CancellationToken);
        var response = await client.PostAsync(
            $"/courses/{Guid.NewGuid()}/publish", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("catalog.request.rate_limited", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Reads_are_not_rate_limited()
    {
        await using var factory = new CatalogApiFactory();
        var client = WithWriteLimit(factory, tokenLimit: 1).CreateClient();

        for (var i = 0; i < 20; i++)
        {
            var response = await client.GetAsync("/courses", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Invalid_write_limit_fails_at_startup()
    {
        await using var factory = new CatalogApiFactory();
        var invalid = WithWriteLimit(factory, tokenLimit: 0);

        Assert.Throws<OptionsValidationException>(() => invalid.CreateClient());
    }

    // A replenishment period far longer than the test, so spent tokens never come back mid-test.
    private static WebApplicationFactory<Program> WithWriteLimit(CatalogApiFactory factory, int tokenLimit) =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("RateLimiting:Writes:TokenLimit", tokenLimit.ToString(CultureInfo.InvariantCulture))
            .UseSetting("RateLimiting:Writes:TokensPerPeriod", "1")
            .UseSetting("RateLimiting:Writes:ReplenishmentPeriod", "01:00:00"));

    private static async Task<HttpResponseMessage> CreateCourseAsync(HttpClient client)
    {
        var request = new { instructorId = Guid.NewGuid(), title = "Rate Limiting in ASP.NET Core", price = 10m, currency = "USD" };
        return await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
