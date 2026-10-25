using Npgsql;

using static LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogRows;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Runs the versioned Catalog read queries against a database of their own, because they read whole tables.
/// These are the results the EF Core repository must reproduce in module 6.
/// </summary>
public sealed class CatalogQueriesTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private static readonly Guid InstructorA = Guid.CreateVersion7();
    private static readonly Guid InstructorB = Guid.CreateVersion7();

    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Public_listing_has_only_published_courses_with_exact_lesson_counts_and_durations_by_title()
    {
        await SeedCatalogAsync();

        var listing = await ListPublishedAsync();

        Assert.Equal(
            [
                ("Clean Architecture", 1L, 480L),
                ("Domain modeling", 0L, 0L),
                ("PostgreSQL in depth", 3L, 1800L),
                ("Testing basics", 2L, 1500L),
            ],
            listing.Select(course => (course.Title, course.LessonCount, course.TotalDurationSeconds)));
    }

    [Fact]
    public async Task Published_course_without_lessons_is_listed_with_zero_lessons_and_zero_duration()
    {
        var courseId = await InsertCourseAsync(_db, title: "Domain modeling", status: "published");

        var course = Assert.Single(await ListPublishedAsync());

        Assert.Equal(courseId, course.Id);
        Assert.Equal(0, course.LessonCount);
        Assert.Equal(0, course.TotalDurationSeconds);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("archived")]
    public async Task Unpublished_course_is_not_in_the_public_listing(string status)
    {
        var courseId = await InsertCourseAsync(_db, status: status);
        await InsertLessonAsync(_db, courseId, position: 1);

        Assert.Empty(await ListPublishedAsync());
    }

    [Fact]
    public async Task Public_listing_returns_the_course_columns_the_catalog_page_shows()
    {
        var courseId = await InsertCourseAsync(
            _db, title: "Clean Architecture", slug: "clean-architecture", priceAmount: 49.99m, priceCurrency: "EUR",
            status: "published");
        await InsertLessonAsync(_db, courseId, position: 1, durationSeconds: 480);

        var course = Assert.Single(await ListPublishedAsync());

        // count and sum over integer both return bigint, which maps to long.
        Assert.Equal(["bigint", "bigint"], await TotalsColumnTypesAsync("list_published_courses.sql"));
        Assert.Equal(
            new CourseSummary(courseId, "Clean Architecture", "clean-architecture", null, 49.99m, "EUR", 1, 480),
            course);
    }

    [Fact]
    public async Task Courses_with_the_same_title_are_ordered_by_slug()
    {
        await InsertCourseAsync(_db, title: "Clean Architecture", slug: "clean-architecture-b", status: "published");
        await InsertCourseAsync(_db, title: "Clean Architecture", slug: "clean-architecture-a", status: "published");
        await InsertCourseAsync(_db, title: "Clean Architecture", slug: "clean-architecture-c", status: "published");

        var listing = await ListPublishedAsync();

        Assert.Equal(
            ["clean-architecture-a", "clean-architecture-b", "clean-architecture-c"],
            listing.Select(course => course.Slug));
    }

    [Fact]
    public async Task Instructor_listing_has_every_course_of_that_instructor_in_any_status_by_title()
    {
        await SeedCatalogAsync();

        var listing = await ListInstructorCoursesAsync(InstructorA);

        Assert.Equal(
            [
                ("Clean Architecture", "published", 1L, 480L),
                ("Event sourcing", "draft", 0L, 0L),
                ("Legacy APIs", "archived", 1L, 240L),
                ("PostgreSQL in depth", "published", 3L, 1800L),
            ],
            listing.Select(course => (course.Title, course.Status, course.LessonCount, course.TotalDurationSeconds)));
    }

    [Fact]
    public async Task Instructor_without_courses_gets_an_empty_listing()
    {
        await SeedCatalogAsync();

        Assert.Empty(await ListInstructorCoursesAsync(Guid.CreateVersion7()));
    }

    /// <summary>
    /// Instructor A: two published courses with lessons, a draft without lessons and an archived course.
    /// Instructor B: a published course with lessons and a published course without any.
    /// Titles differ in their first letter, so the order is the same under any collation.
    /// </summary>
    private async Task SeedCatalogAsync()
    {
        var postgres = await InsertCourseAsync(_db, instructorId: InstructorA, title: "PostgreSQL in depth", status: "published");
        await InsertLessonAsync(_db, postgres, position: 1, durationSeconds: 300);
        await InsertLessonAsync(_db, postgres, position: 2, durationSeconds: 600);
        await InsertLessonAsync(_db, postgres, position: 3, durationSeconds: 900);

        var clean = await InsertCourseAsync(_db, instructorId: InstructorA, title: "Clean Architecture", status: "published");
        await InsertLessonAsync(_db, clean, position: 1, durationSeconds: 480);

        await InsertCourseAsync(_db, instructorId: InstructorA, title: "Event sourcing", status: "draft");

        var legacy = await InsertCourseAsync(_db, instructorId: InstructorA, title: "Legacy APIs", status: "archived");
        await InsertLessonAsync(_db, legacy, position: 1, durationSeconds: 240);

        var testing = await InsertCourseAsync(_db, instructorId: InstructorB, title: "Testing basics", status: "published");
        await InsertLessonAsync(_db, testing, position: 1, durationSeconds: 600);
        await InsertLessonAsync(_db, testing, position: 2, durationSeconds: 900);

        await InsertCourseAsync(_db, instructorId: InstructorB, title: "Domain modeling", status: "published");
    }

    private Task<List<CourseSummary>> ListPublishedAsync() =>
        QueryAsync("list_published_courses.sql", parameter: null, hasStatus: false);

    private Task<List<CourseSummary>> ListInstructorCoursesAsync(Guid instructorId) =>
        QueryAsync("list_instructor_courses.sql", instructorId, hasStatus: true);

    private async Task<string[]> TotalsColumnTypesAsync(string file)
    {
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync(file));
        await using var row = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        return
        [
            row.GetDataTypeName(row.GetOrdinal("lesson_count")),
            row.GetDataTypeName(row.GetOrdinal("total_duration_seconds")),
        ];
    }

    private async Task<List<CourseSummary>> QueryAsync(string file, Guid? parameter, bool hasStatus)
    {
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync(file));
        if (parameter is { } value)
        {
            command.Parameters.AddWithValue(value);
        }

        await using var row = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var courses = new List<CourseSummary>();
        while (await row.ReadAsync(TestContext.Current.CancellationToken))
        {
            courses.Add(new CourseSummary(
                Id: row.GetGuid(row.GetOrdinal("id")),
                Title: row.GetString(row.GetOrdinal("title")),
                Slug: row.GetString(row.GetOrdinal("slug")),
                Status: hasStatus ? row.GetString(row.GetOrdinal("status")) : null,
                PriceAmount: row.GetDecimal(row.GetOrdinal("price_amount")),
                PriceCurrency: row.GetString(row.GetOrdinal("price_currency")),
                LessonCount: row.GetInt64(row.GetOrdinal("lesson_count")),
                TotalDurationSeconds: row.GetInt64(row.GetOrdinal("total_duration_seconds"))));
        }

        return courses;
    }

    private sealed record CourseSummary(
        Guid Id,
        string Title,
        string Slug,
        string? Status,
        decimal PriceAmount,
        string PriceCurrency,
        long LessonCount,
        long TotalDurationSeconds);
}
