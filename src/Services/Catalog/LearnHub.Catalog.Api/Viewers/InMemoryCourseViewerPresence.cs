namespace LearnHub.Catalog.Api.Viewers;

/// <summary>
/// An in-process <see cref="ICourseViewerPresence"/>. Counts are per instance: with several instances each one
/// only sees its own connections. A backplane such as Azure SignalR Service fixes <em>broadcast</em> across
/// instances, not <em>counting</em>, so scaling out also needs a shared presence store (for example Redis).
/// </summary>
public sealed class InMemoryCourseViewerPresence : ICourseViewerPresence
{
    // Both directions, so a disconnect finds its courses without scanning every course.
    private readonly Dictionary<string, HashSet<Guid>> _coursesByConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, HashSet<string>> _connectionsByCourse = [];
    private readonly Lock _gate = new();

    public int Add(string connectionId, Guid courseId)
    {
        lock (_gate)
        {
            GetOrAdd(_coursesByConnection, connectionId).Add(courseId);

            var connections = GetOrAdd(_connectionsByCourse, courseId);
            // A set, so watching twice on one connection counts once.
            connections.Add(connectionId);
            return connections.Count;
        }
    }

    public int Remove(string connectionId, Guid courseId)
    {
        lock (_gate)
        {
            if (_coursesByConnection.TryGetValue(connectionId, out var courses))
            {
                courses.Remove(courseId);
                if (courses.Count == 0)
                {
                    _coursesByConnection.Remove(connectionId);
                }
            }

            return RemoveFromCourse(connectionId, courseId);
        }
    }

    public IReadOnlyDictionary<Guid, int> RemoveConnection(string connectionId)
    {
        lock (_gate)
        {
            if (!_coursesByConnection.Remove(connectionId, out var courses))
            {
                return new Dictionary<Guid, int>();
            }

            return courses.ToDictionary(courseId => courseId, courseId => RemoveFromCourse(connectionId, courseId));
        }
    }

    public int Count(Guid courseId)
    {
        lock (_gate)
        {
            return _connectionsByCourse.TryGetValue(courseId, out var connections) ? connections.Count : 0;
        }
    }

    // Callers hold the lock. Empty sets are dropped, so courses nobody watches do not accumulate.
    private int RemoveFromCourse(string connectionId, Guid courseId)
    {
        if (!_connectionsByCourse.TryGetValue(courseId, out var connections))
        {
            return 0;
        }

        connections.Remove(connectionId);
        if (connections.Count == 0)
        {
            _connectionsByCourse.Remove(courseId);
        }

        return connections.Count;
    }

    private static HashSet<TValue> GetOrAdd<TKey, TValue>(Dictionary<TKey, HashSet<TValue>> map, TKey key)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var set))
        {
            set = [];
            map[key] = set;
        }

        return set;
    }
}
