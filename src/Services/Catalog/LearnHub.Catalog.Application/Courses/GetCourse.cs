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
    string Status);

/// <summary>
/// Handles <see cref="GetCourse"/>. The query reads through the repository until module 6,
/// when it switches to a DbContext projection that skips the aggregate (ADR 0004).
/// </summary>
public sealed class GetCourseHandler(ICourseRepository courses)
{
    public async Task<CourseDetails?> HandleAsync(GetCourse query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var course = await courses.GetAsync(query.CourseId, cancellationToken).ConfigureAwait(false);

        return course is null
            ? null
            : new CourseDetails(
                course.Id,
                course.InstructorId,
                course.Title.Value,
                course.Slug,
                course.Price.Amount,
                course.Price.Currency,
                course.Status.ToString());
    }
}
