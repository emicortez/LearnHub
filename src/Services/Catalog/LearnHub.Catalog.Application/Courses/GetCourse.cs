namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Reads the details of one course.
/// </summary>
public sealed record GetCourse(Guid CourseId);

/// <summary>
/// A read model of a course: plain data, no behavior, safe to hand to any caller.
/// </summary>
public sealed record CourseDetails(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Slug,
    decimal Price,
    string Currency,
    string Status,
    uint Version);

/// <summary>
/// Handles <see cref="GetCourse"/> with a projection that skips the aggregate (ADR 0004).
/// </summary>
public sealed class GetCourseHandler(ICourseQueries courses)
{
    public Task<CourseDetails?> HandleAsync(GetCourse query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return courses.GetAsync(query.CourseId, cancellationToken);
    }
}
