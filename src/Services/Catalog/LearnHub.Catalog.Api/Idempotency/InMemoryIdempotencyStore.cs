namespace LearnHub.Catalog.Api.Idempotency;

/// <summary>
/// An in-process <see cref="IIdempotencyStore"/>. It only protects requests that reach the same instance;
/// Redis replaces it in module 9 so every instance shares the keys.
/// </summary>
public sealed class InMemoryIdempotencyStore(TimeProvider timeProvider, TimeSpan timeToLive) : IIdempotencyStore
{
    public static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromHours(24);

    private readonly Dictionary<string, StoredEntry> _entries = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public InMemoryIdempotencyStore(TimeProvider timeProvider)
        : this(timeProvider, DefaultTimeToLive)
    {
    }

    public Task<IdempotencyEntry?> TryBeginAsync(string key, string fingerprint, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            // Expired entries are dropped when their key is touched again; Redis evicts them on its own.
            if (_entries.TryGetValue(key, out var existing) && existing.ExpiresAt > timeProvider.GetUtcNow())
            {
                return Task.FromResult<IdempotencyEntry?>(existing.Entry);
            }

            _entries[key] = new StoredEntry(new IdempotencyEntry(fingerprint, null), ExpiresAt());
            return Task.FromResult<IdempotencyEntry?>(null);
        }
    }

    public Task CompleteAsync(string key, IdempotentResponse response, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                _entries[key] = new StoredEntry(existing.Entry with { Response = response }, ExpiresAt());
            }
        }

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string key, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _entries.Remove(key);
        }

        return Task.CompletedTask;
    }

    private DateTimeOffset ExpiresAt() => timeProvider.GetUtcNow() + timeToLive;

    private sealed record StoredEntry(IdempotencyEntry Entry, DateTimeOffset ExpiresAt);
}
