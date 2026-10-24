using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Adds the browser security headers to every response of every API, errors included. As a startup filter it wraps
/// the whole pipeline, so it runs before any middleware an API adds and no response can skip it.
/// </summary>
/// <remarks>
/// The headers are written when the response starts, not when the request arrives: the exception handler clears the
/// headers of a failed response before writing its 500, and anything set earlier would be lost.
/// That is also why HSTS can be sent from here behind the ingress: by then UseForwardedHeaders has restored the https
/// scheme of the original request. The Container Apps ingress cannot add response headers itself, so the app owns HSTS.
/// X-XSS-Protection is deliberately absent: it is obsolete, browsers removed the filter, and in some it opened new holes.
/// </remarks>
internal sealed class SecurityHeadersStartupFilter(IWebHostEnvironment environment) : IStartupFilter
{
    // The APIs serve only JSON: if a response is ever opened as a document, it may load nothing and be framed by no one.
    private const string ApiContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    // One year, the usual value once HTTPS works everywhere. No includeSubDomains or preload: those commit the whole
    // domain, a decision for whoever owns it, not for one API.
    private const string StrictTransportSecurity = "max-age=31536000";

    // The interactive OpenAPI UI, mapped only in Development, needs its own scripts and styles.
    private static readonly PathString ScalarPath = new("/scalar");

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Response.OnStarting(() =>
            {
                AddSecurityHeaders(context);
                return Task.CompletedTask;
            });

            return nextMiddleware(context);
        });

        next(app);
    };

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        // Superseded by frame-ancestors; kept for browsers that predate it.
        headers.XFrameOptions = "DENY";

        if (!(environment.IsDevelopment() && context.Request.Path.StartsWithSegments(ScalarPath)))
        {
            headers.ContentSecurityPolicy = ApiContentSecurityPolicy;
        }

        // Browsers ignore HSTS received over http. On localhost a browser would remember it and force https on every local
        // app for a year, which is why Development never gets it either.
        if (context.Request.IsHttps && !environment.IsDevelopment() && !IsLoopback(context.Request.Host))
        {
            headers.StrictTransportSecurity = StrictTransportSecurity;
        }

        // A response to a caller with credentials, or about them, may hold personal data: no browser or proxy keeps it.
        // Anonymous public reads stay cacheable.
        if (context.Request.Headers.ContainsKey(HeaderNames.Authorization)
            || context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            headers.CacheControl = "no-store";
        }
    }

    private static bool IsLoopback(HostString host) =>
        string.Equals(host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
        // Browsers resolve every *.localhost name to loopback too.
        || host.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
        || (System.Net.IPAddress.TryParse(host.Host, out var address) && System.Net.IPAddress.IsLoopback(address));
}
