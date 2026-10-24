using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.IdentityModel.Tokens;

namespace LearnHub.Catalog.Api.Tests;

public sealed class AuthenticationTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private static readonly object CourseRequest = new { title = "Authentication in ASP.NET Core", price = 10m, currency = "USD" };

    [Fact]
    public async Task Create_course_without_a_token_returns_401_with_a_bearer_challenge()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Publish_course_without_a_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/courses/{Guid.NewGuid()}/publish", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task Create_course_with_an_invalid_token_returns_401(string reason, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"A token with {reason} was accepted: {response.StatusCode}.");
    }

    public static TheoryData<string, string> InvalidTokens()
    {
        var otherKey = new SigningCredentials(new RsaSecurityKey(RSA.Create(2048)) { KeyId = "other-key" }, SecurityAlgorithms.RsaSha256);
        var symmetricKey = new SigningCredentials(
            new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)), SecurityAlgorithms.HmacSha256);
        var userId = Guid.NewGuid();

        return new TheoryData<string, string>
        {
            { "an expired lifetime", TestTokens.For(userId, expires: DateTime.UtcNow.AddMinutes(-5)) },
            { "a signature from an unknown key", TestTokens.For(userId, credentials: otherKey) },
            { "an HS256 signature", TestTokens.For(userId, credentials: symmetricKey) },
            { "another audience", TestTokens.For(userId, audience: "api://learnhub-enrollment") },
            { "another issuer", TestTokens.For(userId, issuer: "https://attacker.example/") },
            { "a malformed value", "not-a-jwt" },
        };
    }

    [Fact]
    public async Task Create_course_with_a_token_without_a_user_id_returns_403()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.For(Guid.NewGuid(), includeUserId: false));

        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_instructor_is_the_token_user_and_an_instructor_id_in_the_body_is_ignored()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(userId);
        var request = new { instructorId = Guid.NewGuid(), title = "Claims in ASP.NET Core", price = 10m, currency = "USD" };

        var created = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var course = await client.GetFromJsonAsync<JsonElement>(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(userId, course.GetProperty("instructorId").GetGuid());
    }

    [Fact]
    public async Task Reads_do_not_require_a_token()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/courses", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
