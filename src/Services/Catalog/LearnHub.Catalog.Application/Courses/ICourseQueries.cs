using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// The read side of Catalog: projections to plain data that skip the aggregate (ADR 0004). Implemented by
/// Infrastructure. It returns finished results, never an IQueryable, so a query can neither be reshaped nor run after
/// its context is gone. Anything that decides a write loads the aggregate through <see cref="ICourseRepository"/>.
/// </summary>
public interface ICourseQueries
{
    /// <summary>
    /// A published course, or any course of <paramref name="viewerId"/>. Null for anything else, as for an unknown id,
    /// so a course that is not public cannot be told from one that does not exist.
    /// </summary>
    Task<CourseDetails?> GetAsync(Guid id, Guid? viewerId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns up to <paramref name="take"/> courses with an id greater than <paramref name="afterId"/>
    /// (all of them when it is null), ordered by id. Only published courses and those of <paramref name="viewerId"/>.
    /// </summary>
    Task<IReadOnlyList<CourseSummary>> ListAsync(
        Guid? viewerId,
        Guid? afterId,
        int take,
        CancellationToken cancellationToken);

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

    /// <summary>
    /// The lessons of a published course in position order. Empty for a draft, an archived or an unknown course.
    /// </summary>
    Task<IReadOnlyList<OutlineLesson>> GetPublishedOutlineAsync(Guid courseId, CancellationToken cancellationToken);

    /// <summary>
    /// Up to three published courses per instructor, longest first, by instructor then rank.
    /// </summary>
    Task<IReadOnlyList<LongestCourse>> ListLongestPublishedAsync(CancellationToken cancellationToken);
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
/// A lesson in a course outline, with the second it starts at and its share of the course.
/// </summary>
public sealed record OutlineLesson(
    Guid Id,
    int Position,
    string Title,
    int DurationSeconds,
    long StartOffsetSeconds,
    decimal PercentageOfCourse,
    long TotalDurationSeconds);

/// <summary>
/// A course in its instructor's top three. Rank starts at 1, and the slug breaks ties.
/// </summary>
public sealed record LongestCourse(
    Guid InstructorId,
    Guid Id,
    string Title,
    string Slug,
    long TotalDurationSeconds,
    long Rank);

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
