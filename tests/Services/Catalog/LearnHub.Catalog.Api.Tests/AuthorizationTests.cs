using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LearnHub.Catalog.Api.Tests;

public sealed class AuthorizationTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private static readonly object CourseRequest = new { title = "Authorization in ASP.NET Core", price = 10m, currency = "USD" };

    [Theory]
    [MemberData(nameof(TokensThatCannotWrite))]
    public async Task Create_course_is_forbidden_for(string who, string token)
    {
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{who} got {response.StatusCode}, expected 403.");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(TokensThatCannotWrite))]
    public async Task Publish_course_is_forbidden_for(string who, string token)
    {
        var courseId = await CreateCourseAsync(Guid.NewGuid());
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsync($"/courses/{courseId}/publish", content: null, TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{who} got {response.StatusCode}, expected 403.");
    }

    public static TheoryData<string, string> TokensThatCannotWrite()
    {
        var userId = Guid.NewGuid();

        return new TheoryData<string, string>
        {
            { "a student", TestTokens.For(userId, roles: ["student"]) },
            { "a user without roles", TestTokens.For(userId, roles: []) },
            // Client Credentials: the application's own token, with roles but no delegated scope.
            { "an app-only token", TestTokens.For(userId, scope: null) },
            { "a token without the write scope", TestTokens.For(userId, scope: "courses.read") },
            // Scopes and roles match whole, exact values: look-alikes must not pass.
            { "a scope that only starts with the write scope", TestTokens.For(userId, scope: "courses.writex") },
            { "a scope that only ends with the write scope", TestTokens.For(userId, scope: "xcourses.write") },
            { "comma-separated scopes", TestTokens.For(userId, scope: "courses.read,courses.write") },
            { "the role in another case", TestTokens.For(userId, roles: ["Instructor"]) },
        };
    }

    [Fact]
    public async Task An_instructor_cannot_publish_another_instructors_course_and_it_stays_a_draft()
    {
        var ownerId = Guid.NewGuid();
        var courseId = await CreateCourseAsync(ownerId);
        var otherInstructor = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await otherInstructor.PostAsync(
            $"/courses/{courseId}/publish", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("catalog.course.not_owner", problem.GetProperty("code").GetString());
        Assert.Equal("Forbidden", problem.GetProperty("title").GetString());

        var course = await factory.CreateAuthenticatedClient(ownerId)
            .GetFromJsonAsync<JsonElement>($"/courses/{courseId}", TestContext.Current.CancellationToken);
        Assert.Equal("Draft", course.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_owner_reaches_the_publishing_rules()
    {
        var ownerId = Guid.NewGuid();
        var courseId = await CreateCourseAsync(ownerId);

        var response = await factory.CreateAuthenticatedClient(ownerId)
            .PostAsync($"/courses/{courseId}/publish", content: null, TestContext.Current.CancellationToken);

        // Past authorization, the domain decides: a course without lessons cannot be published yet.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("catalog.course.no_lessons", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_role_among_several_is_enough()
    {
        var client = factory.CreateClientWithToken(TestTokens.For(Guid.NewGuid(), roles: ["student", "instructor"]));

        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task The_write_scope_among_several_is_enough()
    {
        var client = factory.CreateClientWithToken(TestTokens.For(Guid.NewGuid(), scope: "courses.read courses.write"));

        var response = await client.PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> CreateCourseAsync(Guid ownerId)
    {
        var response = await factory.CreateAuthenticatedClient(ownerId)
            .PostAsJsonAsync("/courses", CourseRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetGuid();
    }
}
