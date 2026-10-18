using LearnHub.Catalog.Api.Idempotency;

namespace LearnHub.Catalog.Api.Tests;

public sealed class InMemoryIdempotencyStoreTests
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromHours(24);

    [Fact]
    public async Task A_completed_key_is_replayed_until_it_expires()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryIdempotencyStore(time, TimeToLive);
        var response = new IdempotentResponse(201, "/courses/1", "application/json", """{"id":1}""");
        await store.TryBeginAsync("key", "fingerprint", TestContext.Current.CancellationToken);
        await store.CompleteAsync("key", response, TestContext.Current.CancellationToken);

        time.Advance(TimeToLive - TimeSpan.FromSeconds(1));
        var beforeExpiry = await store.TryBeginAsync("key", "fingerprint", TestContext.Current.CancellationToken);

        Assert.Equal(response, beforeExpiry?.Response);

        time.Advance(TimeSpan.FromSeconds(1));
        var afterExpiry = await store.TryBeginAsync("key", "fingerprint", TestContext.Current.CancellationToken);

        Assert.Null(afterExpiry);
    }

    [Fact]
    public async Task An_in_flight_key_blocks_a_second_claim_until_it_is_released()
    {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);

        var first = await store.TryBeginAsync("key", "fingerprint", TestContext.Current.CancellationToken);
        var second = await store.TryBeginAsync("key", "fingerprint", TestContext.Current.CancellationToken);
        await store.ReleaseAsync("key", TestContext.Current.CancellationToken);
        var afterRelease = await store.TryBeginAsync("key", "fingerprint", TestContext.Current.CancellationToken);

        Assert.Null(first);
        Assert.NotNull(second);
        Assert.Null(second.Response);
        Assert.Null(afterRelease);
    }

    // Minimal stand-in for Microsoft.Extensions.TimeProvider.Testing, which this repo does not reference.
    private sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
