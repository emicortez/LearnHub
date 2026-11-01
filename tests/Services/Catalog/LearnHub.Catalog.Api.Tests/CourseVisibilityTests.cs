using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Application;
using LearnHub.Catalog.Application.Courses;

using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// Reads are public for published courses only. Anything else exists just for its owner: to anyone else it is a 404,
/// the same answer as an id that does not exist.
/// </summary>
public sealed class CourseVisibilityTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact]
    public async Task A_draft_is_not_found_without_a_token()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateDraftAsync(owner);

        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(factory.CreateClient(), id)).StatusCode);
    }

    [Fact]
    public async Task A_draft_is_not_found_for_another_user()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var other = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateDraftAsync(owner);

        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(other, id)).StatusCode);
    }

    [Fact]
    public async Task The_owner_reads_their_draft_with_its_etag()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateDraftAsync(owner);

        var response = await GetAsync(owner, id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task What_the_owner_read_is_never_served_to_anyone_else_from_the_cache()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateDraftAsync(owner);

        Assert.Equal(HttpStatusCode.OK, (await GetAsync(owner, id)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(factory.CreateClient(), id)).StatusCode);
    }

    [Fact]
    public async Task A_course_turns_public_as_soon_as_it_is_published()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var anonymous = factory.CreateClient();
        var id = await CreateDraftAsync(owner, withLesson: true);
        Assert.DoesNotContain(id, await ListAsync(anonymous));

        var published = await owner.PublishAsync(id);

        Assert.Equal(HttpStatusCode.NoContent, published.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync(anonymous, id)).StatusCode);
        Assert.Contains(id, await ListAsync(anonymous));
    }

    [Fact]
    public async Task An_archived_course_is_visible_only_to_its_owner()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateDraftAsync(owner, withLesson: true);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PublishAsync(id)).StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var course = await scope.ServiceProvider.GetRequiredService<ICourseRepository>()
                .GetAsync(id, cancellationToken);
            Assert.True(course!.Archive().IsSuccess);
            var saved = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            Assert.True(saved.IsSuccess);
        }

        Assert.Equal(HttpStatusCode.OK, (await GetAsync(owner, id)).StatusCode);
        Assert.DoesNotContain(id, await ListAsync(factory.CreateAuthenticatedClient(Guid.NewGuid())));
    }

    [Fact]
    public async Task The_list_shows_a_user_their_own_courses_besides_the_published_ones_and_nobody_elses_drafts()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var other = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var mine = await CreateDraftAsync(owner);
        var theirs = await CreateDraftAsync(other);
        var published = await CreateDraftAsync(other, withLesson: true);
        Assert.Equal(HttpStatusCode.NoContent, (await other.PublishAsync(published)).StatusCode);

        var listed = await ListAsync(owner);

        Assert.Contains(mine, listed);
        Assert.Contains(published, listed);
        Assert.DoesNotContain(theirs, listed);
    }

    [Fact]
    public async Task An_invalid_token_reads_like_no_token()
    {
        var owner = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateDraftAsync(owner);
        var forged = factory.CreateClientWithToken(TestTokens.WithoutSignature(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(forged, id)).StatusCode);
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, Guid id) =>
        client.GetAsync($"/courses/{id}", TestContext.Current.CancellationToken);

    private static async Task<List<Guid>> ListAsync(HttpClient client)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ids = new List<Guid>();
        string? cursor = null;
        do
        {
            var path = cursor is null ? "/courses?limit=100" : $"/courses?limit=100&cursor={cursor}";
            var page = await client.GetFromJsonAsync<JsonElement>(path, cancellationToken);
            ids.AddRange(page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
            cursor = page.TryGetProperty("nextCursor", out var next) ? next.GetString() : null;
        }
        while (cursor is not null);

        return ids;
    }

    private async Task<Guid> CreateDraftAsync(HttpClient client, bool withLesson = false)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new { title = $"Visibility {Guid.NewGuid()}", price = 10m, currency = "USD" };
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
}
