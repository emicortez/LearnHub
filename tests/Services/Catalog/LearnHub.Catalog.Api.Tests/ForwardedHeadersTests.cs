using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

// Behind the ingress every request arrives from the proxy over plain HTTP. These tests stand in for the proxy by
// setting the connection's remote IP, then read back what the app believes about the original client: the scheme
// and IP that logs, generated links and IP-based limits see.
public sealed class ForwardedHeadersTests
{
    private const string TrustedNetwork = "10.0.0.0/8";
    private const string EchoPath = "/test/connection";
    private static readonly IPAddress TrustedProxy = IPAddress.Parse("10.0.0.5");
    private static readonly IPAddress UntrustedCaller = IPAddress.Parse("198.51.100.7");

    [Fact]
    public async Task A_trusted_proxy_forwards_the_client_ip_and_scheme()
    {
        await using var factory = new CatalogApiFactory();

        var seen = await EchoAsync(factory, TrustedProxy, forwardedFor: "203.0.113.1");

        Assert.Equal("https 203.0.113.1", seen);
    }

    [Fact]
    public async Task Forwarded_headers_from_an_untrusted_caller_are_ignored()
    {
        await using var factory = new CatalogApiFactory();

        var seen = await EchoAsync(factory, UntrustedCaller, forwardedFor: "203.0.113.1");

        Assert.Equal($"http {UntrustedCaller}", seen);
    }

    [Fact]
    public async Task Spoofed_entries_before_the_proxy_appended_address_are_ignored()
    {
        await using var factory = new CatalogApiFactory();

        // The client prepends a fake address; the proxy appends the real one. Only the right-most entry counts.
        var seen = await EchoAsync(factory, TrustedProxy, forwardedFor: "6.6.6.6, 203.0.113.1");

        Assert.Equal("https 203.0.113.1", seen);
    }

    [Fact]
    public async Task Links_use_the_original_scheme_from_a_trusted_proxy()
    {
        await using var factory = new CatalogApiFactory();
        var client = CreateHost(factory, TrustedProxy).CreateAuthenticatedClient(Guid.NewGuid());
        using var request = new HttpRequestMessage(HttpMethod.Post, "/courses")
        {
            Content = JsonContent.Create(new { title = "Forwarded Headers in ASP.NET Core", price = 10m, currency = "USD" }),
        };
        request.Headers.Add("X-Forwarded-For", "203.0.113.1");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Uri.UriSchemeHttps, response.Headers.Location?.Scheme);
    }

    [Fact]
    public async Task Hsts_is_sent_for_the_original_https_request_behind_a_trusted_proxy()
    {
        await using var factory = new CatalogApiFactory();
        var client = CreateHost(factory, TrustedProxy)
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://catalog.learnhub.test") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/courses");
        request.Headers.Add("X-Forwarded-For", "203.0.113.1");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(["max-age=31536000"], response.Headers.GetValues("Strict-Transport-Security"));
    }

    private static async Task<string> EchoAsync(CatalogApiFactory factory, IPAddress remoteIp, string forwardedFor)
    {
        var client = CreateHost(factory, remoteIp).CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, EchoPath);
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static WebApplicationFactory<Program> CreateHost(CatalogApiFactory factory, IPAddress remoteIp) =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("ForwardedHeaders:KnownNetworks:0", TrustedNetwork)
            .ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ProxyStartupFilter(remoteIp))));

    // Wraps the app's pipeline. Before it, the connection comes from the given IP, as if from the proxy. After it, a
    // request no endpoint matched falls through to an echo of the scheme and IP the app ended up believing.
    private sealed class ProxyStartupFilter(IPAddress remoteIp) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = remoteIp;
                return nextMiddleware(context);
            });
            next(app);
            app.Run(context => context.Request.Path == EchoPath
                ? context.Response.WriteAsync($"{context.Request.Scheme} {context.Connection.RemoteIpAddress}")
                : Task.CompletedTask);
        };
    }
}
