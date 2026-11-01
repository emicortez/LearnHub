using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Publishes a draft course so students can find it. <paramref name="RequestedBy"/> is the signed-in user,
/// taken from the validated token, never from the request body. <paramref name="ExpectedVersion"/> is the version
/// the decision was based on; null when the client sent one that can never match.
/// </summary>
public sealed record PublishCourse(Guid CourseId, Guid RequestedBy, uint? ExpectedVersion);

/// <summary>
/// Handles <see cref="PublishCourse"/>: loads the aggregate, checks that the requester owns it, and lets it enforce
/// the publishing rules. Ownership is a business rule, so it lives here rather than in HTTP: it holds whatever
/// delivers the command, an endpoint today or a message later.
/// The repository has no Update: the context tracks the loaded course, and the unit of work commits the change.
/// The version is compared before the rules, so a client on a stale read learns that, not a state it has not seen.
/// A write that lands after this check is caught by the commit, whose UPDATE requires the version loaded here.
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

        if (courses.GetVersion(course) != command.ExpectedVersion)
        {
            return CourseErrors.VersionMismatch;
        }

        var published = course.Publish();

        return published.IsSuccess
            ? await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false)
            : published;
    }
}
