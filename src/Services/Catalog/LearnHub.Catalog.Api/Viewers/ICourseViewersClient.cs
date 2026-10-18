namespace LearnHub.Catalog.Api.Viewers;

/// <summary>
/// The messages the server pushes to clients watching a course page. A strongly typed client, so a renamed
/// method is a compile error instead of a message no client listens for.
/// </summary>
public interface ICourseViewersClient
{
    Task ViewerCountChanged(Guid courseId, int count);
}
