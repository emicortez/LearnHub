using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;

using LearnHub.Catalog.Api.Viewers;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace LearnHub.Catalog.Api.Tests;

// A fresh host per test, so presence kept by one test never leaks into another's counts.
public sealed class CourseViewersHubTests : IAsyncDisposable
{
    private readonly CatalogApiFactory _factory = new();
    private readonly List<Viewer> _viewers = [];

    [Fact]
    public async Task A_single_viewer_receives_a_count_of_one()
    {
        var courseId = await CreateCourseAsync();
        var viewer = await ConnectAsync();

        await viewer.WatchAsync(courseId);

        Assert.Equal((courseId, 1), await viewer.NextAsync());
    }

    [Fact]
    public async Task A_second_viewer_makes_both_viewers_receive_two()
    {
        var courseId = await CreateCourseAsync();
        var first = await ConnectAsync();
        var second = await ConnectAsync();
        await first.WatchAsync(courseId);
        Assert.Equal((courseId, 1), await first.NextAsync());

        await second.WatchAsync(courseId);

        Assert.Equal((courseId, 2), await first.NextAsync());
        Assert.Equal((courseId, 2), await second.NextAsync());
    }

    [Fact]
    public async Task When_a_viewer_disconnects_the_others_receive_the_lower_count()
    {
        var courseId = await CreateCourseAsync();
        var first = await ConnectAsync();
        var second = await ConnectAsync();
        await first.WatchAsync(courseId);
        await second.WatchAsync(courseId);
        Assert.Equal((courseId, 1), await first.NextAsync());
        Assert.Equal((courseId, 2), await first.NextAsync());

        await second.DisposeAsync();

        Assert.Equal((courseId, 1), await first.NextAsync());
    }

    [Fact]
    public async Task Watching_the_same_course_twice_on_one_connection_counts_once()
    {
        var courseId = await CreateCourseAsync();
        var first = await ConnectAsync();
        var second = await ConnectAsync();

        await first.WatchAsync(courseId);
        await first.WatchAsync(courseId);

        Assert.Equal((courseId, 1), await first.NextAsync());
        Assert.Equal((courseId, 1), await first.NextAsync());

        await second.WatchAsync(courseId);

        Assert.Equal((courseId, 2), await second.NextAsync());
    }

    [Fact]
    public async Task Watching_a_course_that_does_not_exist_is_rejected_with_a_clear_message()
    {
        var unknownId = Guid.NewGuid();
        var viewer = await ConnectAsync();

        var exception = await Assert.ThrowsAsync<HubException>(() => viewer.WatchAsync(unknownId));

        // The client prefixes the server's HubException message with the method name; any other exception
        // would arrive without it, so finding the message proves the server threw a HubException.
        Assert.Contains($"HubException: Course {unknownId} does not exist.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Viewers_of_one_course_do_not_receive_counts_of_another()
    {
        var courseA = await CreateCourseAsync();
        var courseB = await CreateCourseAsync();
        var viewerOfA = await ConnectAsync();
        var firstOfB = await ConnectAsync();
        var secondOfB = await ConnectAsync();
        await viewerOfA.WatchAsync(courseA);
        await firstOfB.WatchAsync(courseB);
        await secondOfB.WatchAsync(courseB);
        Assert.Equal((courseB, 2), await secondOfB.NextAsync());

        // Every broadcast for B was sent before this call, so any message for B would arrive ahead of this one.
        await viewerOfA.WatchAsync(courseA);

        Assert.Equal((courseA, 1), await viewerOfA.NextAsync());
        Assert.Equal((courseA, 1), await viewerOfA.NextAsync());
        Assert.False(viewerOfA.HasPending);
    }

    [Fact]
    public async Task Stopping_watching_lowers_the_count()
    {
        var courseId = await CreateCourseAsync();
        var first = await ConnectAsync();
        var second = await ConnectAsync();
        await first.WatchAsync(courseId);
        await second.WatchAsync(courseId);
        Assert.Equal((courseId, 1), await first.NextAsync());
        Assert.Equal((courseId, 2), await first.NextAsync());

        await second.StopWatchingAsync(courseId);

        Assert.Equal((courseId, 1), await first.NextAsync());
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var viewer in _viewers)
        {
            await viewer.DisposeAsync();
        }

        await _factory.DisposeAsync();
    }

    private async Task<Guid> CreateCourseAsync()
    {
        var client = _factory.CreateClient();
        var request = new { instructorId = Guid.NewGuid(), title = "Real-time Apps", price = 20m, currency = "USD" };
        var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Viewer> ConnectAsync()
    {
        var server = _factory.Server;
        // WebSockets over the in-memory TestServer: the same transport browsers use, and a disposed client
        // closes the socket, so the server sees the disconnect at once.
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, CourseViewersServiceCollectionExtensions.HubPath.TrimStart('/')), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                    await server.CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken);
            })
            .Build();

        var viewer = new Viewer(connection);
        _viewers.Add(viewer);
        await connection.StartAsync(TestContext.Current.CancellationToken);
        return viewer;
    }

    /// <summary>A connected client that queues every count pushed to it, so tests await them in order.</summary>
    private sealed class Viewer : IAsyncDisposable
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly HubConnection _connection;
        private readonly Channel<(Guid CourseId, int Count)> _received = Channel.CreateUnbounded<(Guid, int)>();

        public Viewer(HubConnection connection)
        {
            _connection = connection;
            _connection.On<Guid, int>(
                nameof(ICourseViewersClient.ViewerCountChanged),
                (courseId, count) => _received.Writer.TryWrite((courseId, count)));
        }

        public bool HasPending => _received.Reader.TryPeek(out _);

        public Task WatchAsync(Guid courseId) =>
            _connection.InvokeAsync(nameof(CourseViewersHub.WatchCourse), courseId, TestContext.Current.CancellationToken);

        public Task StopWatchingAsync(Guid courseId) =>
            _connection.InvokeAsync(nameof(CourseViewersHub.StopWatching), courseId, TestContext.Current.CancellationToken);

        public async Task<(Guid CourseId, int Count)> NextAsync()
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(Timeout);
            return await _received.Reader.ReadAsync(timeout.Token);
        }

        // Safe to call twice: a disposed HubConnection ignores the second call.
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}
