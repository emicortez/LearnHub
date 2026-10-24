using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace LearnHub.Catalog.Api.Tests;

// The browser enforces CORS; these tests send the headers a browser would and check what Catalog answers. A missing
// Access-Control-Allow-Origin is how the server refuses: the browser then hides the response from the page's script.
public sealed class CorsTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private const string AllowOrigin = "Access-Control-Allow-Origin";
    private const string FrontendOrigin = "https://app.learnhub.dev";
    private const string LocalFrontendOrigin = "http://localhost:5173";

    private static readonly object CourseRequest = new { title = "CORS in ASP.NET Core", price = 10m, currency = "USD" };

    [Fact]
    public async Task An_allowed_origin_can_read_the_response()
    {
        var client = CreateHost().CreateClient();

        var response = await SendAsync(client, HttpMethod.Get, "/courses", FrontendOrigin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([FrontendOrigin], HeaderValues(response, AllowOrigin));
        Assert.Contains("Origin", response.Headers.Vary);
        // Catalog is bearer-only: the browser never needs to send it cookies.
        Assert.Empty(HeaderValues(response, "Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task No_origin_is_allowed_by_default()
    {
        var client = factory.CreateClient();

        var response = await SendAsync(client, HttpMethod.Get, "/courses", FrontendOrigin);

        Assert.Empty(HeaderValues(response, AllowOrigin));
    }

    [Theory]
    [InlineData("https://evil.example")]
    // Look-alikes of an allowed origin: origins match whole and exact, never by prefix, suffix or scheme.
    [InlineData("https://app.learnhub.dev.evil.example")]
    [InlineData("https://evil-app.learnhub.dev")]
    [InlineData("https://evil.app.learnhub.dev")]
    [InlineData("http://app.learnhub.dev")]
    [InlineData("https://app.learnhub.dev:8443")]
    // What a sandboxed iframe or a local file sends.
    [InlineData("null")]
    public async Task An_unknown_origin_gets_no_cors_headers(string origin)
    {
        var client = CreateHost().CreateClient();

        var response = await SendAsync(client, HttpMethod.Get, "/courses", origin);

        // The server still answers; CORS only decides whether the browser lets the page read it.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(HeaderValues(response, AllowOrigin));
    }

    [Fact]
    public async Task The_preflight_allows_an_authenticated_json_write()
    {
        var client = CreateHost().CreateClient();

        var response = await PreflightAsync(client, FrontendOrigin, "POST", "authorization,content-type,idempotency-key");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([FrontendOrigin], HeaderValues(response, AllowOrigin));
        Assert.Contains("POST", ListedValues(response, "Access-Control-Allow-Methods"));
        var allowedHeaders = ListedValues(response, "Access-Control-Allow-Headers");
        Assert.Contains("authorization", allowedHeaders, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("content-type", allowedHeaders, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("idempotency-key", allowedHeaders, StringComparer.OrdinalIgnoreCase);
        Assert.Empty(HeaderValues(response, "Access-Control-Allow-Credentials"));
        Assert.NotEmpty(HeaderValues(response, "Access-Control-Max-Age"));
    }

    // ASP.NET Core answers an allowed origin's preflight with the policy's full lists and the browser compares them with
    // what it asked for, so a refused method or header is one missing from the list. An unknown origin gets nothing.
    [Theory]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task The_preflight_does_not_list_a_method_the_api_does_not_use(string method)
    {
        var client = CreateHost().CreateClient();

        var response = await PreflightAsync(client, FrontendOrigin, method, "content-type");

        Assert.DoesNotContain(method, ListedValues(response, "Access-Control-Allow-Methods"));
    }

    [Fact]
    public async Task The_preflight_does_not_list_a_header_the_api_does_not_use()
    {
        var client = CreateHost().CreateClient();

        var response = await PreflightAsync(client, FrontendOrigin, "POST", "x-custom");

        Assert.DoesNotContain("x-custom", ListedValues(response, "Access-Control-Allow-Headers"), StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("*", ListedValues(response, "Access-Control-Allow-Headers"));
    }

    [Fact]
    public async Task The_preflight_from_an_unknown_origin_gets_no_cors_headers()
    {
        var client = CreateHost().CreateClient();

        var response = await PreflightAsync(client, "https://evil.example", "POST", "authorization,content-type");

        Assert.Empty(HeaderValues(response, AllowOrigin));
        Assert.Empty(HeaderValues(response, "Access-Control-Allow-Methods"));
        Assert.Empty(HeaderValues(response, "Access-Control-Allow-Headers"));
    }

    [Fact]
    public async Task A_rejected_write_still_carries_cors_headers_so_the_page_can_read_why()
    {
        var client = CreateHost().CreateClient();

        var unauthorized = await SendAsync(client, HttpMethod.Post, "/courses", FrontendOrigin, JsonContent.Create(CourseRequest));
        var forbidden = await SendAsync(
            CreateHost().CreateClientWithToken(TestTokens.For(Guid.NewGuid(), roles: ["student"])),
            HttpMethod.Post, "/courses", FrontendOrigin, JsonContent.Create(CourseRequest));

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal([FrontendOrigin], HeaderValues(unauthorized, AllowOrigin));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal([FrontendOrigin], HeaderValues(forbidden, AllowOrigin));
    }

    [Fact]
    public async Task The_page_can_read_the_location_of_a_created_course()
    {
        var client = CreateHost().CreateAuthenticatedClient(Guid.NewGuid());

        var response = await SendAsync(client, HttpMethod.Post, "/courses", FrontendOrigin, JsonContent.Create(CourseRequest));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal([FrontendOrigin], HeaderValues(response, AllowOrigin));
        Assert.Contains("Location", ListedValues(response, "Access-Control-Expose-Headers"), StringComparer.OrdinalIgnoreCase);
    }

    // Anonymous reads are output-cached. A cached response must not carry the origin of the request that filled it.
    [Fact]
    public async Task A_cached_read_answers_each_origin_with_its_own_headers()
    {
        var client = CreateHost().CreateClient();

        await SendAsync(client, HttpMethod.Get, "/courses", FrontendOrigin);
        var otherAllowed = await SendAsync(client, HttpMethod.Get, "/courses", LocalFrontendOrigin);
        var unknown = await SendAsync(client, HttpMethod.Get, "/courses", "https://evil.example");

        Assert.Equal([LocalFrontendOrigin], HeaderValues(otherAllowed, AllowOrigin));
        Assert.Empty(HeaderValues(unknown, AllowOrigin));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.learnhub.dev")]
    [InlineData("https://app.learnhub.dev/")]
    [InlineData("https://app.learnhub.dev/courses")]
    [InlineData("https://app.learnhub.dev?x=1")]
    [InlineData("app.learnhub.dev")]
    [InlineData("ftp://app.learnhub.dev")]
    [InlineData("https://user@app.learnhub.dev")]
    [InlineData("")]
    public void An_origin_that_is_not_exact_stops_the_service_at_startup(string origin)
    {
        var host = factory.WithWebHostBuilder(builder => builder.UseSetting("Cors:AllowedOrigins:0", origin));

        var exception = Assert.Throws<OptionsValidationException>(() => host.CreateClient());

        Assert.Contains("Cors:AllowedOrigins", exception.Message, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("Cors:AllowedOrigins:0", FrontendOrigin)
            .UseSetting("Cors:AllowedOrigins:1", LocalFrontendOrigin));

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, string origin, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("Origin", origin);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> PreflightAsync(HttpClient client, string origin, string method, string headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/courses");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", headers);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // A CORS header may list several values in one comma-separated string.
    private static string[] ListedValues(HttpResponseMessage response, string name) =>
        [.. HeaderValues(response, name)
            .SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))];

    private static string[] HeaderValues(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? [.. values] : [];
}
