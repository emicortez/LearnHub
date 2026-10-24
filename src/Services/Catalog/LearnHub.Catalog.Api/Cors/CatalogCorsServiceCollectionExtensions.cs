using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace LearnHub.Catalog.Api.Cors;

public static class CatalogCorsServiceCollectionExtensions
{
    public const string FrontendPolicy = "frontend";

    public static IServiceCollection AddCatalogCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrontendCorsOptions>()
            .Bind(configuration.GetSection(FrontendCorsOptions.SectionName))
            .Validate(
                options => options.AllowedOrigins.All(IsExactOrigin),
                "Every entry in Cors:AllowedOrigins must be an exact origin such as https://app.learnhub.dev: an http or "
                + "https scheme, a host and an optional non-default port, with no path, trailing slash, query or wildcard.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<FrontendCorsOptions>>((cors, frontend) =>
                cors.AddPolicy(FrontendPolicy, policy => policy
                    .WithOrigins(frontend.Value.AllowedOrigins)
                    // Only what the API uses. No AllowCredentials: Catalog is bearer-only and never reads a cookie.
                    .WithMethods(HttpMethods.Get, HttpMethods.Post)
                    .WithHeaders("Authorization", "Content-Type", "Idempotency-Key")
                    // Response headers a script cannot read unless they are exposed: where a created course lives, and
                    // how long to wait after a 429.
                    .WithExposedHeaders("Location", "Retry-After")
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

        return services;
    }

    // An origin is compared whole, so the configured value must already be the exact string a browser sends.
    // "*" would allow every origin, and anything else that is not an exact origin would never match or would match by
    // accident.
    private static bool IsExactOrigin(string? value) =>
        !string.IsNullOrEmpty(value)
        && !value.Contains('*', StringComparison.Ordinal)
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.UserInfo.Length == 0
        && string.Equals(value, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
}
