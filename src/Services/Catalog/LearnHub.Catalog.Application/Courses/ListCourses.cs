namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Reads one page of courses, ordered by id, starting after <paramref name="After"/> (keyset pagination).
/// </summary>
public sealed record ListCourses(int Limit, Guid? After);

/// <summary>
/// The short form of a course shown in lists.
/// </summary>
public sealed record CourseSummary(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string Currency,
    string Status);

/// <summary>
/// One page of course summaries. <see cref="NextAfter"/> is the id to continue after, or null on the last page.
/// </summary>
public sealed record CoursePage(IReadOnlyList<CourseSummary> Items, Guid? NextAfter);

/// <summary>
/// Handles <see cref="ListCourses"/>. It asks for one row more than the limit: if that row exists,
/// there is a next page, and the last item returned is where it starts.
/// </summary>
public sealed class ListCoursesHandler(ICourseRepository courses)
{
    public async Task<CoursePage> HandleAsync(ListCourses query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.Limit);

        var rows = await courses.ListAsync(query.After, query.Limit + 1, cancellationToken).ConfigureAwait(false);

        var hasNextPage = rows.Count > query.Limit;
        var items = rows
            .Take(query.Limit)
            .Select(course => new CourseSummary(
                course.Id,
                course.Title.Value,
                course.Slug,
                course.Price.Amount,
                course.Price.Currency,
                course.Status.ToString()))
            .ToList();

        return new CoursePage(items, hasNextPage ? items[^1].Id : null);
    }
}
