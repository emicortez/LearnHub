using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// The read side of Catalog: projections to plain data that skip the aggregate (ADR 0004). Implemented by
/// Infrastructure. It returns finished results, never an IQueryable, so a query can neither be reshaped nor run after
/// its context is gone. Anything that decides a write loads the aggregate through <see cref="ICourseRepository"/>.
/// </summary>
public interface ICourseQueries
{
    Task<CourseDetails?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns up to <paramref name="take"/> courses with an id greater than <paramref name="afterId"/>
    /// (all of them when it is null), ordered by id.
    /// </summary>
    Task<IReadOnlyList<CourseSummary>> ListAsync(Guid? afterId, int take, CancellationToken cancellationToken);

    /// <summary>
    /// The public catalog: up to <paramref name="take"/> published courses by title then slug, starting after
    /// <paramref name="after"/> (from the first one when it is null).
    /// </summary>
    Task<IReadOnlyList<PublishedCourse>> ListPublishedAsync(
        PublishedCourseCursor? after,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// One instructor's courses in every status, by title then slug.
    /// </summary>
    Task<IReadOnlyList<InstructorCourse>> ListByInstructorAsync(Guid instructorId, CancellationToken cancellationToken);
}

/// <summary>
/// A course in the public catalog. The totals are 0 for a course without lessons.
/// </summary>
public sealed record PublishedCourse(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string Currency,
    long LessonCount,
    long TotalDurationSeconds);

/// <summary>
/// The last row of a catalog page. It is the whole sort key: titles repeat, so the title alone would skip courses.
/// </summary>
public sealed record PublishedCourseCursor(CourseTitle Title, string Slug);

/// <summary>
/// A course in its instructor's list, in any status. The totals are 0 for a course without lessons.
/// </summary>
public sealed record InstructorCourse(
    Guid Id,
    string Title,
    string Slug,
    string Status,
    decimal Price,
    string Currency,
    long LessonCount,
    long TotalDurationSeconds);
