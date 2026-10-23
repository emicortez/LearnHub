using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

// Behind the ingress every request arrives from the proxy over plain HTTP. These tests stand in for the proxy by
// setting the connection's remote IP, then check what the API believes about the original client.
public sealed class ForwardedHeadersTests
{
    private const string TrustedNetwork = "10.0.0.0/8";
    private static readonly IPAddress TrustedProxy = IPAddress.Parse("10.0.0.5");
    private static readonly IPAddress UntrustedCaller = IPAddress.Parse("198.51.100.7");

    [Fact]
    public async Task Clients_behind_a_trusted_proxy_get_their_own_write_bucket()
    {
        await using var factory = new CatalogApiFactory();
        var client = CreateClient(factory, remoteIp: TrustedProxy, tokenLimit: 1);

        var first = await CreateCourseAsync(client, forwardedFor: "203.0.113.1");
        var second = await CreateCourseAsync(client, forwardedFor: "203.0.113.2");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Forwarded_headers_from_an_untrusted_caller_are_ignored()
    {
        await using var factory = new CatalogApiFactory();
        var client = CreateClient(factory, remoteIp: UntrustedCaller, tokenLimit: 1);

        var first = await CreateCourseAsync(client, forwardedFor: "203.0.113.1");
        // A spoofed address must not buy a fresh bucket: the caller is still limited by its real IP.
        var second = await CreateCourseAsync(client, forwardedFor: "203.0.113.2");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task Links_use_the_original_scheme_from_a_trusted_proxy()
    {
        await using var factory = new CatalogApiFactory();
        var client = CreateClient(factory, remoteIp: TrustedProxy, tokenLimit: 10);

        var response = await CreateCourseAsync(client, forwardedFor: "203.0.113.1", forwardedProto: "https");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Uri.UriSchemeHttps, response.Headers.Location?.Scheme);
    }

    [Fact]
    public async Task Links_keep_the_connection_scheme_for_an_untrusted_caller()
    {
        await using var factory = new CatalogApiFactory();
        var client = CreateClient(factory, remoteIp: UntrustedCaller, tokenLimit: 10);

        var response = await CreateCourseAsync(client, forwardedFor: "203.0.113.1", forwardedProto: "https");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Uri.UriSchemeHttp, response.Headers.Location?.Scheme);
    }

    private static HttpClient CreateClient(CatalogApiFactory factory, IPAddress remoteIp, int tokenLimit) =>
        factory.WithWebHostBuilder(builder => builder
                .UseSetting("ForwardedHeaders:KnownNetworks:0", TrustedNetwork)
                .UseSetting("RateLimiting:Writes:TokenLimit", tokenLimit.ToString(CultureInfo.InvariantCulture))
                .UseSetting("RateLimiting:Writes:TokensPerPeriod", "1")
                .UseSetting("RateLimiting:Writes:ReplenishmentPeriod", "01:00:00")
                .ConfigureServices(services => services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(remoteIp))))
            .CreateClient();

    private static async Task<HttpResponseMessage> CreateCourseAsync(
        HttpClient client, string forwardedFor, string? forwardedProto = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/courses")
        {
            Content = JsonContent.Create(new
            {
                instructorId = Guid.NewGuid(),
                title = "Forwarded Headers in ASP.NET Core",
                price = 10m,
                currency = "USD",
            }),
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        if (forwardedProto is not null)
        {
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // Startup filters run before the app's own pipeline, so this sets the IP the forwarded headers middleware sees.
    private sealed class RemoteIpStartupFilter(IPAddress remoteIp) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = remoteIp;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
