using System.Net.Http.Headers;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// Issues access tokens the way the identity provider would, signed with a key only the tests know.
/// The API is configured to trust this issuer and key instead of discovering the real provider.
/// </summary>
public static class TestTokens
{
    public const string Issuer = "https://identity.test.learnhub.local/";
    public const string Audience = "api://learnhub-catalog";

    public static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "test-key" };

    private static readonly SigningCredentials Credentials = new(SigningKey, SecurityAlgorithms.RsaSha256);

    public static string For(
        Guid userId,
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null,
        DateTime? notBefore = null,
        SigningCredentials? credentials = null,
        bool includeUserId = true,
        bool includeExpiry = true)
    {
        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object>();
        if (includeUserId)
        {
            claims["oid"] = userId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = now.AddMinutes(-10),
            NotBefore = notBefore ?? now.AddMinutes(-10),
            Expires = includeExpiry ? expires ?? now.AddMinutes(10) : null,
            SigningCredentials = credentials ?? Credentials,
        };

        // Without this the handler fills in a missing exp, so a token without one could not be issued.
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = includeExpiry }.CreateToken(descriptor);
    }

    /// <summary>
    /// A token with "alg": "none" and no signature: valid JSON claims that anyone could have written.
    /// </summary>
    public static string WithoutSignature(Guid userId)
    {
        var signed = For(userId);
        var payload = signed.Split('.')[1];
        var header = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
        return $"{header}.{payload}.";
    }

    public static HttpClient CreateAuthenticatedClient(this WebApplicationFactory<Program> factory, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", For(userId));
        return client;
    }
}
