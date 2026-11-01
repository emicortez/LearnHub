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
/// The repository has no Update: the context tracks the loaded course, and the unit of work commits the change.
/// </summary>
public sealed class PublishCourseHandler(ICourseRepository courses, IUnitOfWork unitOfWork)
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
        if (course.InstructorId != command.RequestedBy)
        {
            return CourseErrors.NotOwner;
        }

        var published = course.Publish();

        return published.IsSuccess
            ? await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false)
            : published;
    }
}
