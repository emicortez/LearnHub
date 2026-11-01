using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// The collection of <see cref="Course"/> aggregates. Implemented by Infrastructure (ADR 0003).
/// Changes to loaded aggregates are tracked and saved by the unit of work, so there is no Update or Save here.
/// </summary>
public interface ICourseRepository
{
    Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken);

    void Add(Course course);

    /// <summary>
    /// The version of a course this repository loaded or saved. It changes with each write to the course itself;
    /// adding a lesson does not change it, because lessons are stored apart.
    /// </summary>
    uint GetVersion(Course course);
}
