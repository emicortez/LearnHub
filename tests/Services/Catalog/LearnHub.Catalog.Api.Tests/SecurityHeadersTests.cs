using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LearnHub.Catalog.Api.Tests;

// Error responses reach the browser too, so every status must carry the same headers as a successful read.
public sealed class SecurityHeadersTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private const string ApiContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
    private const string Hsts = "max-age=31536000";

    private static readonly object CourseRequest = new { title = "Security Headers", price = 10m, currency = "USD" };

    [Theory]
    [InlineData("/courses")]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task A_successful_read_carries_the_security_headers(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task A_401_carries_the_security_headers()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertSecurityHeaders(response);
        AssertNoStore(response);
    }

    [Fact]
    public async Task A_403_carries_the_security_headers()
    {
        var response = await factory.CreateClientWithToken(TestTokens.For(Guid.NewGuid(), roles: ["student"]))
            .PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertSecurityHeaders(response);
        AssertNoStore(response);
    }

    [Theory]
    [InlineData("/courses/00000000-0000-0000-0000-000000000001")]
    [InlineData("/no-such-route")]
    public async Task A_404_carries_the_security_headers(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task A_429_carries_the_security_headers()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder
                .UseSetting("RateLimiting:Writes:TokenLimit", "1")
                .UseSetting("RateLimiting:Writes:TokensPerPeriod", "1"))
            .CreateAuthenticatedClient(Guid.NewGuid());

        await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);
        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        AssertSecurityHeaders(response);
        AssertNoStore(response);
    }

    [Fact]
    public async Task A_500_carries_the_security_headers()
    {
        // The exception handler clears the headers of the failed response before writing the 500.
        var client = factory
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Production")
                .ConfigureTestServices(services =>
                    services.Replace(ServiceDescriptor.Singleton<ICourseRepository, ThrowingCourseRepository>())))
            .CreateClient();

        var response = await client.GetAsync($"/courses/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task An_authenticated_response_is_never_stored_by_a_browser_or_proxy()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var created = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);
        var read = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        AssertNoStore(created);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        AssertNoStore(read);
    }

    [Fact]
    public async Task An_anonymous_public_read_stays_cacheable()
    {
        var response = await factory.CreateClient().GetAsync("/courses", TestContext.Current.CancellationToken);

        Assert.False(response.Headers.CacheControl?.NoStore ?? false, "An anonymous public read must not be no-store.");
    }

    // Behind the ingress the request arrives over plain HTTP; the forwarded headers restore https before the response
    // starts (ForwardedHeadersTests covers that hop). Here the test client calls over https directly.
    [Fact]
    public async Task An_https_response_outside_development_tells_the_browser_to_use_https_from_now_on()
    {
        var response = await HttpsClient("Production", "https://catalog.learnhub.test")
            .GetAsync("/courses", TestContext.Current.CancellationToken);

        Assert.Equal([Hsts], Values(response, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task An_error_over_https_also_carries_hsts()
    {
        var response = await HttpsClient("Production", "https://catalog.learnhub.test")
            .PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([Hsts], Values(response, "Strict-Transport-Security"));
    }

    [Theory]
    // A browser would remember it for localhost and force https on every local app for a year.
    [InlineData("Development", "https://catalog.learnhub.test")]
    [InlineData("Production", "https://localhost")]
    [InlineData("Production", "https://127.0.0.1")]
    // Browsers ignore HSTS received over http, and an attacker in the middle could strip it anyway.
    [InlineData("Production", "http://catalog.learnhub.test")]
    public async Task No_hsts_in_development_on_loopback_or_over_http(string environment, string baseAddress)
    {
        var response = await HttpsClient(environment, baseAddress).GetAsync("/courses", TestContext.Current.CancellationToken);

        Assert.Empty(Values(response, "Strict-Transport-Security"));
    }

    [Fact]
    public void Kestrel_does_not_announce_itself_in_a_server_header()
    {
        // The test server never adds a Server header, so the setting itself is what can be checked.
        var kestrel = factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        Assert.False(kestrel.AddServerHeader);
    }

    [Fact]
    public async Task Scalar_in_development_can_load_its_scripts_and_styles()
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();

        var response = await client.GetAsync("/scalar", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Content-Security-Policy"), "The API's CSP would block Scalar's UI.");
        Assert.Equal(["nosniff"], response.Headers.GetValues("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Outside_development_the_scalar_path_gets_the_api_policy()
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        var response = await client.GetAsync("/scalar", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    private HttpClient HttpsClient(string environment, string baseAddress) =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment))
            .CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(baseAddress) });

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal(["nosniff"], Values(response, "X-Content-Type-Options"));
        Assert.Equal(["no-referrer"], Values(response, "Referrer-Policy"));
        Assert.Equal([ApiContentSecurityPolicy], Values(response, "Content-Security-Policy"));
        Assert.Equal(["DENY"], Values(response, "X-Frame-Options"));
        // Obsolete: browsers removed the filter, and in some it opened new holes.
        Assert.Empty(Values(response, "X-XSS-Protection"));
        Assert.Empty(Values(response, "Server"));
    }

    private static void AssertNoStore(HttpResponseMessage response) =>
        Assert.True(
            response.Headers.CacheControl?.NoStore ?? false,
            string.Create(CultureInfo.InvariantCulture, $"{(int)response.StatusCode} should be Cache-Control: no-store."));

    private static string[] Values(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? [.. values] : [];

    private sealed class ThrowingCourseRepository : ICourseRepository
    {
        public Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Repository failure.");

        public Task<IReadOnlyList<Course>> ListAsync(Guid? afterId, int take, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Repository failure.");

        public void Add(Course course) => throw new InvalidOperationException("Repository failure.");
    }
}
