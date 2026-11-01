using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LearnHub.Catalog.Api.Tests;

// Each test builds its own host, so it starts with an empty cache and its own read counter.
public sealed class OutputCachingTests
{
    [Fact]
    public async Task A_second_identical_detail_read_is_served_from_the_cache()
    {
        await using var host = new CachingHost();
        var id = await host.CreatePublishedCourseAsync("Caching Basics");

        await host.GetJsonAsync($"/courses/{id}");
        await host.GetJsonAsync($"/courses/{id}");

        Assert.Equal(1, host.Reads.Count);
    }

    [Fact]
    public async Task A_second_identical_list_read_is_served_from_the_cache()
    {
        await using var host = new CachingHost();
        await host.CreatePublishedCourseAsync("Caching Lists");

        await host.GetJsonAsync("/courses?limit=10");
        await host.GetJsonAsync("/courses?limit=10");

        Assert.Equal(1, host.Reads.Count);
    }

    [Fact]
    public async Task Different_limit_and_cursor_values_are_cached_separately()
    {
        await using var host = new CachingHost();
        await host.CreatePublishedCourseAsync("Course A");
        await host.CreatePublishedCourseAsync("Course B");
        var firstPage = await host.GetJsonAsync("/courses?limit=1");
        var cursor = Uri.EscapeDataString(firstPage.GetProperty("nextCursor").GetString()!);

        await host.GetJsonAsync("/courses?limit=2");
        await host.GetJsonAsync($"/courses?limit=1&cursor={cursor}");
        // Repeats of the three distinct reads above, all served from the cache.
        await host.GetJsonAsync("/courses?limit=1");
        await host.GetJsonAsync("/courses?limit=2");
        await host.GetJsonAsync($"/courses?limit=1&cursor={cursor}");

        Assert.Equal(3, host.Reads.Count);
    }

    [Fact]
    public async Task A_course_published_after_a_list_read_appears_in_the_next_identical_list_read()
    {
        await using var host = new CachingHost();
        var before = await host.GetJsonAsync("/courses?limit=100");
        Assert.Empty(before.GetProperty("items").EnumerateArray());

        var id = await host.CreatePublishedCourseAsync("Fresh Course");
        var after = await host.GetJsonAsync("/courses?limit=100");

        Assert.Contains(after.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
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

        Assert.Equal(1, host.Reads.Count);
    }

    private sealed class CachingHost : IAsyncDisposable
    {
        private readonly CatalogApiFactory _factory = new();
        private readonly WebApplicationFactory<Program> _host;

        public CachingHost()
        {
            _host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Scoped<ICourseQueries>(provider =>
                    new CountingCourseQueries(ActivatorUtilities.CreateInstance<CourseQueries>(provider), Reads)))));
            Client = _host.CreateClient();
            Writer = _host.CreateAuthenticatedClient(Guid.NewGuid());
        }

        public ReadCounter Reads { get; } = new();

        // Reads stay anonymous: the output cache skips any request that carries an Authorization header.
        public HttpClient Client { get; }

        public HttpClient Writer { get; }

        // Reads count from here: publishing reads the course first.
        public async Task<Guid> CreatePublishedCourseAsync(string title)
        {
            var request = new { title, price = 10m, currency = "USD" };
            var response = await Writer.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var id = (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
            await _host.PublishWithLessonAsync(Writer, id);
            Reads.Reset();

            return id;
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

    private sealed class ReadCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Add() => Interlocked.Increment(ref _count);

        public void Reset() => Interlocked.Exchange(ref _count, 0);
    }

    // Counts reads that reach the database; a read served from the output cache never gets here.
    private sealed class CountingCourseQueries(ICourseQueries inner, ReadCounter reads) : ICourseQueries
    {
        public Task<CourseDetails?> GetAsync(Guid id, Guid? viewerId, CancellationToken cancellationToken)
        {
            reads.Add();
            return inner.GetAsync(id, viewerId, cancellationToken);
        }

        public Task<IReadOnlyList<CourseSummary>> ListAsync(
            Guid? viewerId,
            Guid? afterId,
            int take,
            CancellationToken cancellationToken)
        {
            reads.Add();
            return inner.ListAsync(viewerId, afterId, take, cancellationToken);
        }

        public Task<IReadOnlyList<PublishedCourse>> ListPublishedAsync(
            PublishedCourseCursor? after,
            int take,
            CancellationToken cancellationToken) =>
            inner.ListPublishedAsync(after, take, cancellationToken);

        public Task<IReadOnlyList<InstructorCourse>> ListByInstructorAsync(
            Guid instructorId,
            CancellationToken cancellationToken) =>
            inner.ListByInstructorAsync(instructorId, cancellationToken);

        public Task<IReadOnlyList<OutlineLesson>> GetPublishedOutlineAsync(
            Guid courseId,
            CancellationToken cancellationToken) =>
            inner.GetPublishedOutlineAsync(courseId, cancellationToken);

        public Task<IReadOnlyList<LongestCourse>> ListLongestPublishedAsync(CancellationToken cancellationToken) =>
            inner.ListLongestPublishedAsync(cancellationToken);
    }
}
