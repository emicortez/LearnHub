using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Application;
using LearnHub.Catalog.Application.Courses;

using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

public sealed class CourseConcurrencyEndpointTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly Guid _instructor = Guid.NewGuid();

    [Fact]
    public async Task Get_course_returns_a_strong_etag()
    {
        var id = await CreateCourseAsync();

        var etag = await GetETagAsync(id);

        Assert.Matches("^\"[0-9]+\"$", etag);
    }

    [Fact]
    public async Task A_cached_read_carries_the_etag_of_the_body_it_serves()
    {
        var id = await CreateCourseAsync(withLesson: true);
        Assert.Equal(HttpStatusCode.NoContent, (await PublishAsync(id, await GetETagAsync(id))).StatusCode);
        using var anonymous = factory.CreateClient();

        var fresh = await anonymous.GetAsync($"/courses/{id}", TestContext.Current.CancellationToken);
        var cached = await anonymous.GetAsync($"/courses/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(await GetETagAsync(id), Assert.Single(fresh.Headers.GetValues("ETag")));
        Assert.Equal(fresh.Headers.ETag, cached.Headers.ETag);
    }

    [Fact]
    public async Task Publish_with_the_current_etag_returns_204_and_the_etag_changes()
    {
        var id = await CreateCourseAsync(withLesson: true);
        var etag = await GetETagAsync(id);

        var response = await PublishAsync(id, etag);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotEqual(etag, await GetETagAsync(id));
        Assert.Equal("Published", await GetStatusAsync(id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("*")]
    public async Task Publish_without_a_specific_etag_returns_428_and_changes_nothing(string? ifMatch)
    {
        var id = await CreateCourseAsync(withLesson: true);

        var response = await PublishAsync(id, ifMatch);

        await AssertProblemAsync(
            response, HttpStatusCode.PreconditionRequired, "Precondition Required", "catalog.course.version_required");
        Assert.Equal("Draft", await GetStatusAsync(id));
    }

    [Fact]
    public async Task Publish_with_a_stale_etag_returns_412_before_any_business_rule()
    {
        var id = await CreateCourseAsync(withLesson: true);
        var stale = await GetETagAsync(id);
        Assert.Equal(HttpStatusCode.NoContent, (await PublishAsync(id, stale)).StatusCode);

        // The course is no longer a draft, so a 409 would tell this client about a state it has not seen.
        var response = await PublishAsync(id, stale);

        await AssertProblemAsync(
            response, HttpStatusCode.PreconditionFailed, "Precondition Failed", "catalog.course.version_mismatch");
    }

    [Theory]
    [InlineData("W/\"1\"")]
    [InlineData("\"1\", \"2\"")]
    [InlineData("\"not-a-version\"")]
    [InlineData("1")]
    public async Task Publish_with_an_etag_that_cannot_match_returns_412_and_changes_nothing(string ifMatch)
    {
        var id = await CreateCourseAsync(withLesson: true);

        var response = await PublishAsync(id, ifMatch);

        await AssertProblemAsync(
            response, HttpStatusCode.PreconditionFailed, "Precondition Failed", "catalog.course.version_mismatch");
        Assert.Equal("Draft", await GetStatusAsync(id));
    }

    [Fact]
    public async Task Publish_with_a_weak_form_of_the_current_etag_returns_412()
    {
        var id = await CreateCourseAsync(withLesson: true);

        var response = await PublishAsync(id, $"W/{await GetETagAsync(id)}");

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task Publish_with_another_spelling_of_the_current_etag_returns_412()
    {
        var id = await CreateCourseAsync(withLesson: true);
        var etag = await GetETagAsync(id);

        // Entity tags compare as opaque strings, so this is a tag the API never issued.
        var response = await PublishAsync(id, etag.Insert(1, "0"));

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task A_current_etag_still_gets_the_409_of_a_business_rule()
    {
        var id = await CreateCourseAsync();

        var response = await PublishAsync(id, await GetETagAsync(id));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conflict", "catalog.course.no_lessons");
    }

    [Fact]
    public async Task An_unknown_course_is_a_404_whatever_the_etag()
    {
        var response = await PublishAsync(Guid.NewGuid(), "\"1\"");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Not Found", "catalog.course.not_found");
    }

    [Fact]
    public async Task Another_instructor_gets_403_rather_than_the_state_of_the_version()
    {
        var id = await CreateCourseAsync(withLesson: true);
        using var other = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await PublishAsync(id, "\"1\"", other);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "Forbidden", "catalog.course.not_owner");
    }

    [Fact]
    public async Task Two_simultaneous_publishes_with_one_etag_let_one_win_and_the_other_gets_412()
    {
        var id = await CreateCourseAsync(withLesson: true);
        var etag = await GetETagAsync(id);

        var responses = await Task.WhenAll(PublishAsync(id, etag), PublishAsync(id, etag));

        Assert.Equal(
            [HttpStatusCode.NoContent, HttpStatusCode.PreconditionFailed],
            responses.Select(response => response.StatusCode).Order());
    }

    private async Task<Guid> CreateCourseAsync(bool withLesson = false)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateAuthenticatedClient(_instructor);
        var request = new { title = $"Concurrency {Guid.NewGuid():N}", price = 25m, currency = "USD" };
        var created = await client.PostAsJsonAsync("/courses", request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("id").GetGuid();

        if (withLesson)
        {
            // The API cannot add lessons yet, so the lesson is saved directly.
            await using var scope = factory.Services.CreateAsyncScope();
            var course = await scope.ServiceProvider.GetRequiredService<ICourseRepository>()
                .GetAsync(id, cancellationToken);
            Assert.True(course!.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
            var saved = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            Assert.True(saved.IsSuccess);
        }

        return id;
    }

    private async Task<string> GetETagAsync(Guid id)
    {
        // Authenticated, because a draft is visible only to its owner.
        using var client = factory.CreateAuthenticatedClient(_instructor);
        var response = await client.GetAsync($"/courses/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return Assert.Single(response.Headers.GetValues("ETag"));
    }

    private async Task<string?> GetStatusAsync(Guid id)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Authenticated, because the output cache skips requests with an Authorization header.
        using var client = factory.CreateAuthenticatedClient(_instructor);
        var response = await client.GetAsync($"/courses/{id}", cancellationToken);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken))
            .GetProperty("status")
            .GetString();
    }

    private async Task<HttpResponseMessage> PublishAsync(Guid id, string? ifMatch, HttpClient? client = null)
    {
        using var owner = factory.CreateAuthenticatedClient(_instructor);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/courses/{id}/publish");
        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return await (client ?? owner).SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string title,
        string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(title, problem.GetProperty("title").GetString());
        Assert.Equal(code, problem.GetProperty("code").GetString());
    }
}
