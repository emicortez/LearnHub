using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Publishes a draft course so students can find it.
/// </summary>
public sealed record PublishCourse(Guid CourseId);

/// <summary>
/// Handles <see cref="PublishCourse"/>: loads the aggregate and lets it enforce the publishing rules.
/// There is no explicit save: the in-memory repository holds the same instance, and from module 6
/// EF Core's unit of work saves the tracked changes.
/// </summary>
public sealed class PublishCourseHandler(ICourseRepository courses)
{
    public async Task<Result> HandleAsync(PublishCourse command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await courses.GetAsync(command.CourseId, cancellationToken).ConfigureAwait(false);

        return course is null
            ? CourseErrors.NotFound
            : course.Publish();
    }
}
