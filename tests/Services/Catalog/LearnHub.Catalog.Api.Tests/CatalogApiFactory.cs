using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// The shared host for API tests. Write buckets are per user and tests may reuse one, so the limit is raised far above
/// what any test sends and they are never throttled.
/// Rate-limit tests lower it again with <c>WithWebHostBuilder</c>.
/// Tokens come from <see cref="TestTokens"/>: the issuer and audience are set through the same configuration a real
/// environment uses, and only the signing key replaces the provider's discovery document.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("RateLimiting:Writes:TokenLimit", "100000");
        builder.UseSetting("RateLimiting:Writes:TokensPerPeriod", "100000");

        builder.UseSetting("Authentication:Schemes:Bearer:ValidIssuer", TestTokens.Issuer);
        builder.UseSetting("Authentication:Schemes:Bearer:ValidAudiences:0", TestTokens.Audience);

        builder.ConfigureServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.SigningKey;
            }));
    }
}
