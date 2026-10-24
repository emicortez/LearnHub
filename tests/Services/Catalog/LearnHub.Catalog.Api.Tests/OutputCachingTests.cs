using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure.Courses;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LearnHub.Catalog.Api.Tests;

// Each test builds its own host, so it starts with an empty cache and its own counting repository.
public sealed class OutputCachingTests
{
    [Fact]
    public async Task A_second_identical_detail_read_is_served_from_the_cache()
    {
        await using var host = new CachingHost();
        var id = await host.CreateCourseAsync("Caching Basics");

        await host.GetJsonAsync($"/courses/{id}");
        await host.GetJsonAsync($"/courses/{id}");

        Assert.Equal(1, host.Repository.Reads);
    }

    [Fact]
    public async Task A_second_identical_list_read_is_served_from_the_cache()
    {
        await using var host = new CachingHost();
        await host.CreateCourseAsync("Caching Lists");

        await host.GetJsonAsync("/courses?limit=10");
        await host.GetJsonAsync("/courses?limit=10");

        Assert.Equal(1, host.Repository.Reads);
    }

    [Fact]
    public async Task Different_limit_and_cursor_values_are_cached_separately()
    {
        await using var host = new CachingHost();
        await host.CreateCourseAsync("Course A");
        await host.CreateCourseAsync("Course B");
        var firstPage = await host.GetJsonAsync("/courses?limit=1");
        var cursor = Uri.EscapeDataString(firstPage.GetProperty("nextCursor").GetString()!);

        await host.GetJsonAsync("/courses?limit=2");
        await host.GetJsonAsync($"/courses?limit=1&cursor={cursor}");
        // Repeats of the three distinct reads above, all served from the cache.
        await host.GetJsonAsync("/courses?limit=1");
        await host.GetJsonAsync("/courses?limit=2");
        await host.GetJsonAsync($"/courses?limit=1&cursor={cursor}");

        Assert.Equal(3, host.Repository.Reads);
    }

    [Fact]
    public async Task A_course_created_after_a_list_read_appears_in_the_next_identical_list_read()
    {
        await using var host = new CachingHost();
        var before = await host.GetJsonAsync("/courses?limit=100");
        Assert.Empty(before.GetProperty("items").EnumerateArray());

        var id = await host.CreateCourseAsync("Fresh Course");
        var after = await host.GetJsonAsync("/courses?limit=100");

        Assert.Contains(after.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task A_published_course_reads_as_published_after_being_read_as_a_draft()
    {
        await using var host = new CachingHost();
        var id = await host.CreateCourseAsync("Publish Me");
        var draft = await host.GetJsonAsync($"/courses/{id}");
        Assert.Equal("Draft", draft.GetProperty("status").GetString());

        // The API cannot add lessons yet, so the lesson goes on the aggregate the singleton repository holds;
        // the publish itself goes through the API, and it is the write whose eviction is under test.
        var course = await host.Repository.Inner.GetAsync(id, TestContext.Current.CancellationToken);
        Assert.True(course!.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        var publish = await host.Writer.PostAsync(
            $"/courses/{id}/publish", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);

        var published = await host.GetJsonAsync($"/courses/{id}");

        Assert.Equal("Published", published.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_failed_create_does_not_evict_the_cache()
    {
        await using var host = new CachingHost();
        await host.GetJsonAsync("/courses?limit=10");

        var request = new { title = "", price = 10m, currency = "USD" };
        var failed = await host.Writer.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, failed.StatusCode);
        await host.GetJsonAsync("/courses?limit=10");

        Assert.Equal(1, host.Repository.Reads);
    }

    private sealed class CachingHost : IAsyncDisposable
    {
        private readonly CatalogApiFactory _factory = new();
        private readonly WebApplicationFactory<Program> _host;

        public CachingHost()
        {
            _host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Singleton<ICourseRepository>(Repository))));
            Client = _host.CreateClient();
            Writer = _host.CreateAuthenticatedClient(Guid.NewGuid());
        }

        public CountingCourseRepository Repository { get; } = new(new InMemoryCourseRepository());

        // Reads stay anonymous: the output cache skips any request that carries an Authorization header.
        public HttpClient Client { get; }

        public HttpClient Writer { get; }

        public async Task<Guid> CreateCourseAsync(string title)
        {
            var request = new { title, price = 10m, currency = "USD" };
            var response = await Writer.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public async Task<JsonElement> GetJsonAsync(string path)
        {
            var response = await Client.GetAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await ReadJsonAsync(response);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _host.DisposeAsync();
            await _factory.DisposeAsync();
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    // Counts reads that reach the repository; a read served from the output cache never gets here.
    private sealed class CountingCourseRepository(InMemoryCourseRepository inner) : ICourseRepository
    {
        private int _reads;

        public InMemoryCourseRepository Inner => inner;

        public int Reads => Volatile.Read(ref _reads);

        public Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _reads);
            return inner.GetAsync(id, cancellationToken);
        }

        public Task<IReadOnlyList<Course>> ListAsync(Guid? afterId, int take, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _reads);
            return inner.ListAsync(afterId, take, cancellationToken);
        }

        public void Add(Course course) => inner.Add(course);
    }
}
