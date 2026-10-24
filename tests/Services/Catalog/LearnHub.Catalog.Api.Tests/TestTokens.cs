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
        SigningCredentials? credentials = null,
        bool includeUserId = true)
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
            NotBefore = now.AddMinutes(-10),
            Expires = expires ?? now.AddMinutes(10),
            SigningCredentials = credentials ?? Credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static HttpClient CreateAuthenticatedClient(this WebApplicationFactory<Program> factory, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", For(userId));
        return client;
    }
}
