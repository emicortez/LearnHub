using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace LearnHub.Catalog.Api.Authentication;

/// <summary>
/// Catalog is a resource server: it validates access tokens from the identity provider (ADR 0009) and never
/// sees a password. Authority and audiences come from <c>Authentication:Schemes:Bearer</c>, so every environment
/// configures its own tenant; the checks that must hold everywhere are fixed here.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    // Entra's 'sub' is pairwise: the same user gets a different value in each application, so Catalog and
    // Enrollment would disagree on it. 'oid' is the user's id across the tenant. With another provider this is 'sub'.
    public const string UserIdClaim = "oid";

    // Entra sends app roles as a "roles" array and delegated scopes as one space-separated "scp" string.
    public const string RolesClaim = "roles";
    public const string ScopeClaim = "scp";

    public static IServiceCollection AddCatalogAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep JWT claim names as issued; otherwise 'oid' is renamed to a long legacy SOAP URI.
                options.MapInboundClaims = false;

                // The API decides the algorithm, never the token's header: this rules out 'none' and HS256 confusion.
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256];
                // The default tolerates five minutes of clock drift, which keeps expired tokens alive too long.
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
                // With inbound mapping off, ASP.NET must be told which claim holds roles, or IsInRole is always false.
                options.TokenValidationParameters.RoleClaimType = RolesClaim;
            });

        services.AddAuthorization(authorization =>
        {
            // RequireAuthorization() without a policy uses this one: a person signed in through an app. Delegated
            // tokens carry scopes; an app-only (Client Credentials) token has none and is not a user here.
            authorization.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.TryGetUserId(out _) && context.User.HasClaim(c => c.Type == ScopeClaim))
                .Build();

            // Two independent checks: the app was granted the write scope, and the person holds the instructor role.
            authorization.AddPolicy(CatalogPolicies.Instructor, policy => policy
                .Combine(authorization.DefaultPolicy)
                .RequireAssertion(context => context.User.HasScope(CatalogPolicies.WriteScope))
                .RequireRole(CatalogPolicies.InstructorRole));
        });

        return services;
    }

    public static bool HasScope(this ClaimsPrincipal user, string scope)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.FindAll(ScopeClaim)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope, StringComparer.Ordinal);
    }

    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(user);

        return Guid.TryParse(user.FindFirstValue(UserIdClaim), out userId) && userId != Guid.Empty;
    }

    /// <summary>
    /// The user id of a request that passed the default policy. Throws if called on an endpoint without it.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        user.TryGetUserId(out var userId)
            ? userId
            : throw new InvalidOperationException("The endpoint does not require an authenticated user with a user id.");
}
