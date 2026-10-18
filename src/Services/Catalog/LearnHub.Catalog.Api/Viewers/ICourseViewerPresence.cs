namespace LearnHub.Catalog.Api.Viewers;

/// <summary>
/// Tracks which connections are watching which courses. A course's viewer count is the number of distinct
/// connections watching it, so watching the same course twice on one connection counts once.
/// Every operation returns the counts it changed, read under the same lock as the change.
/// </summary>
public interface ICourseViewerPresence
{
    /// <summary>Records that the connection watches the course and returns the course's viewer count.</summary>
    int Add(string connectionId, Guid courseId);

    /// <summary>Records that the connection stopped watching the course and returns the course's viewer count.</summary>
    int Remove(string connectionId, Guid courseId);

    /// <summary>
    /// Forgets every course the connection watched and returns the new viewer count of each of them.
    /// </summary>
    IReadOnlyDictionary<Guid, int> RemoveConnection(string connectionId);

    int Count(Guid courseId);
}
