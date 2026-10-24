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
/// Two headers are deliberately absent. Strict-Transport-Security belongs to whatever terminates TLS, the ingress in
/// front of the APIs, never to an app behind it that only speaks plain HTTP. X-XSS-Protection is obsolete: browsers
/// removed the filter, and in some it opened new holes.
/// </remarks>
internal sealed class SecurityHeadersStartupFilter(IWebHostEnvironment environment) : IStartupFilter
{
    // The APIs serve only JSON: if a response is ever opened as a document, it may load nothing and be framed by no one.
    private const string ApiContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

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

        // A response to a caller with credentials, or about them, may hold personal data: no browser or proxy keeps it.
        // Anonymous public reads stay cacheable.
        if (context.Request.Headers.ContainsKey(HeaderNames.Authorization)
            || context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            headers.CacheControl = "no-store";
        }
    }
}
