namespace LearnHub.Catalog.Api.Idempotency;

public static class IdempotencyKeys
{
    /// <summary>
    /// The store key for a client's key. Keys are scoped to the user who sent them: two users picking the same key
    /// are two different operations, and one must never replay the other's response.
    /// </summary>
    public static string ForUser(Guid userId, string key) => $"{userId:N}:{key}";
}
