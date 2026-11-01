using System.Linq.Expressions;

using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure.Persistence.Queries;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Persistence;

/// <summary>
/// Reads Catalog through projections: only the columns each result needs, one command each, nothing tracked.
/// A projection returns no entities, so AsNoTracking would add nothing and AutoInclude does not apply.
/// Title.Value and Status.ToString() run in memory on the column values, which EF Core allows only in the final Select.
/// The window queries run as the versioned SQL in Persistence/Queries: LINQ has no general way to write a window
/// function. Their column aliases are the property names of the results.
/// </summary>
public sealed class CourseQueries(CatalogDbContext context) : ICourseQueries
{
    // Duration is a TimeSpan in the model and an integer column, so TotalSeconds has no translation. The cast through
    // object never runs: it makes EF Core use the column as it is. long keeps the bigint of the versioned queries.
    private static readonly Expression<Func<Lesson, long>> Seconds = lesson => (int)(object)lesson.Duration;

    private static readonly string OutlineSql = QueryFile.Read("get_published_course_outline.sql");

    private static readonly string LongestCoursesSql =
        QueryFile.Read("list_longest_published_courses_per_instructor.sql");

    public Task<CourseDetails?> GetAsync(Guid id, Guid? viewerId, CancellationToken cancellationToken) =>
        VisibleTo(viewerId)
            .Where(course => course.Id == id)
            .Select(course => new CourseDetails(
                course.Id,
                course.InstructorId,
                course.Title.Value,
                course.Slug,
                course.Price.Amount,
                course.Price.Currency,
                course.Status.ToString(),
                EF.Property<uint>(course, CourseConfiguration.Version)))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CourseSummary>> ListAsync(
        Guid? viewerId,
        Guid? afterId,
        int take,
        CancellationToken cancellationToken) =>
        await VisibleTo(viewerId)
            .Where(course => afterId == null || course.Id > afterId)
            .OrderBy(course => course.Id)
            .Take(take)
            .Select(course => new CourseSummary(
                course.Id,
                course.Title.Value,
                course.Slug,
                course.Price.Amount,
                course.Price.Currency,
                course.Status.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PublishedCourse>> ListPublishedAsync(
        PublishedCourseCursor? after,
        int take,
        CancellationToken cancellationToken)
    {
        var published = context.Courses.Where(course => course.Status == CourseStatus.Published);

        if (after is not null)
        {
            // A row comparison, so PostgreSQL starts at the cursor inside ix_courses_published_title_slug.
            published = published.Where(course => EF.Functions.GreaterThan(
                ValueTuple.Create(course.Title, course.Slug),
                ValueTuple.Create(after.Title, after.Slug)));
        }

        return await published
            .OrderBy(course => course.Title)
            .ThenBy(course => course.Slug)
            .Take(take)
            .Select(course => new PublishedCourse(
                course.Id,
                course.Title.Value,
                course.Slug,
                course.Price.Amount,
                course.Price.Currency,
                course.Lessons.LongCount(),
                course.Lessons.AsQueryable().Sum(Seconds)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InstructorCourse>> ListByInstructorAsync(
        Guid instructorId,
        CancellationToken cancellationToken) =>
        await context.Courses
            .Where(course => course.InstructorId == instructorId)
            .OrderBy(course => course.Title)
            .ThenBy(course => course.Slug)
            .Select(course => new InstructorCourse(
                course.Id,
                course.Title.Value,
                course.Slug,
                course.Status.ToString(),
                course.Price.Amount,
                course.Price.Currency,
                course.Lessons.LongCount(),
                course.Lessons.AsQueryable().Sum(Seconds)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    // The parameter is named because EF Core names any parameter it gets: a positional $1 would stay unbound.
    public async Task<IReadOnlyList<OutlineLesson>> GetPublishedOutlineAsync(
        Guid courseId,
        CancellationToken cancellationToken) =>
        await context.Database
            .SqlQueryRaw<OutlineLesson>(OutlineSql, new NpgsqlParameter("course_id", courseId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LongestCourse>> ListLongestPublishedAsync(CancellationToken cancellationToken) =>
        await context.Database
            .SqlQueryRaw<LongestCourse>(LongestCoursesSql)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private IQueryable<Course> VisibleTo(Guid? viewerId) =>
        context.Courses.Where(course => course.Status == CourseStatus.Published || course.InstructorId == viewerId);
}
