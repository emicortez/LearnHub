using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Publishes a draft course so students can find it. <paramref name="RequestedBy"/> is the signed-in user,
/// taken from the validated token, never from the request body.
/// </summary>
public sealed record PublishCourse(Guid CourseId, Guid RequestedBy);

/// <summary>
/// Handles <see cref="PublishCourse"/>: loads the aggregate, checks that the requester owns it, and lets it enforce
/// the publishing rules. Ownership is a business rule, so it lives here rather than in HTTP: it holds whatever
/// delivers the command, an endpoint today or a message later.
/// There is no explicit save: the in-memory repository holds the same instance, and from module 6
/// EF Core's unit of work saves the tracked changes.
/// </summary>
public sealed class PublishCourseHandler(ICourseRepository courses)
{
    public async Task<Result> HandleAsync(PublishCourse command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await courses.GetAsync(command.CourseId, cancellationToken).ConfigureAwait(false);

        if (course is null)
        {
            return CourseErrors.NotFound;
        }

        // Checked before anything changes: someone else's course is never touched.
        return course.InstructorId == command.RequestedBy
            ? course.Publish()
            : CourseErrors.NotOwner;
    }
}
