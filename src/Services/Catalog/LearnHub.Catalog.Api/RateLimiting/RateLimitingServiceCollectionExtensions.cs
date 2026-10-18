using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LearnHub.Catalog.Api.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public const string WritesPolicy = "writes";

    public static IServiceCollection AddCatalogRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WriteRateLimitOptions>()
            .Bind(configuration.GetSection(WriteRateLimitOptions.SectionName))
            .Validate(options => options.TokenLimit > 0, "RateLimiting:Writes:TokenLimit must be greater than zero.")
            .Validate(options => options.TokensPerPeriod > 0, "RateLimiting:Writes:TokensPerPeriod must be greater than zero.")
            .Validate(options => options.ReplenishmentPeriod > TimeSpan.Zero, "RateLimiting:Writes:ReplenishmentPeriod must be positive.")
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            // The default rejection is 503, which tells clients the server is down rather than that they must slow down.
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteRejectionAsync;

            limiter.AddPolicy(WritesPolicy, httpContext =>
            {
                var options = httpContext.RequestServices.GetRequiredService<IOptions<WriteRateLimitOptions>>().Value;

                // One bucket per client IP. QueueLimit 0 rejects at once instead of holding the request open.
                return RateLimitPartition.GetTokenBucketLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = options.TokenLimit,
                        TokensPerPeriod = options.TokensPerPeriod,
                        ReplenishmentPeriod = options.ReplenishmentPeriod,
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }

    // Same shape as every other error: a standard title and a stable 'code' extension clients can branch on.
    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        // The middleware has already set RejectionStatusCode on the response; the body follows it.
        var statusCode = httpContext.Response.StatusCode;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            // Retry-After takes whole seconds; rounding up keeps a client from retrying a moment too early.
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Detail = "Too many write requests. Slow down and retry later.",
                Extensions = { ["code"] = "catalog.request.rate_limited" },
            },
        });
    }
}
