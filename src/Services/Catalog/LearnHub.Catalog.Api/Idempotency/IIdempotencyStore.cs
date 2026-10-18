namespace LearnHub.Catalog.Api.Idempotency;

/// <summary>
/// Remembers idempotency keys and the response each one produced, so a retried request is answered
/// without running the handler again. Every operation is a single atomic step, so a Redis implementation
/// can map them to SET NX, SET and DEL.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically marks the key as in flight for the given fingerprint.
    /// Returns <see langword="null"/> when the caller now owns the key, or the existing entry when it was already taken.
    /// </summary>
    Task<IdempotencyEntry?> TryBeginAsync(string key, string fingerprint, CancellationToken cancellationToken);

    /// <summary>Stores the response of an in-flight key, so later requests with that key replay it.</summary>
    Task CompleteAsync(string key, IdempotentResponse response, CancellationToken cancellationToken);

    /// <summary>Forgets the key, so the client can correct the request and retry with the same key.</summary>
    Task ReleaseAsync(string key, CancellationToken cancellationToken);
}

/// <summary>A taken key: in flight while <see cref="Response"/> is null, completed once it is stored.</summary>
public sealed record IdempotencyEntry(string Fingerprint, IdempotentResponse? Response);

/// <summary>
/// A serializable snapshot of a successful response. Plain values only, so it can be stored outside the process.
/// </summary>
public sealed record IdempotentResponse(int StatusCode, string? Location, string? ContentType, string? Body);
