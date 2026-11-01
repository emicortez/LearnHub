using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using static LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogRows;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Pins the projections on a real database of their own. The versioned SQL in Persistence/Queries is the reference:
/// the listings must return exactly its rows, in its order.
/// </summary>
public sealed class CourseQueriesTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private readonly CommandRecorder _recorder = new();

    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Get_returns_the_course_with_its_status_name_and_the_version_of_its_row()
    {
        var instructorId = Guid.CreateVersion7();
        var id = await InsertCourseAsync(
            _db, instructorId: instructorId, title: "Clean Architecture", slug: "clean-architecture",
            priceAmount: 49.99m, priceCurrency: "EUR", status: "published");
        await InsertLessonAsync(_db, id, position: 1);

        await using var context = NewContext();
        var course = await new CourseQueries(context).GetAsync(id, viewerId: null, TestContext.Current.CancellationToken);

        Assert.Equal(
            new CourseDetails(
                id, instructorId, "Clean Architecture", "clean-architecture", 49.99m, "EUR", "Published",
                await ReadVersionAsync(id)),
            course);
    }

    [Fact]
    public async Task Get_returns_null_for_an_unknown_course()
    {
        await using var context = NewContext();

        Assert.Null(await new CourseQueries(context).GetAsync(Guid.NewGuid(), viewerId: null, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("archived")]
    public async Task An_unpublished_course_exists_only_for_its_owner(string status)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var instructorId = Guid.CreateVersion7();
        var id = await InsertCourseAsync(_db, instructorId: instructorId, status: status);

        await using var context = NewContext();
        var queries = new CourseQueries(context);

        Assert.NotNull(await queries.GetAsync(id, instructorId, cancellationToken));
        Assert.Single(await queries.ListAsync(instructorId, afterId: null, take: 10, cancellationToken));
        Assert.Null(await queries.GetAsync(id, viewerId: null, cancellationToken));
        Assert.Empty(await queries.ListAsync(viewerId: null, afterId: null, take: 10, cancellationToken));
        Assert.Null(await queries.GetAsync(id, Guid.CreateVersion7(), cancellationToken));
        Assert.Empty(await queries.ListAsync(Guid.CreateVersion7(), afterId: null, take: 10, cancellationToken));
    }

    [Fact]
    public async Task Queries_leave_nothing_in_the_change_tracker()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var instructorId = Guid.CreateVersion7();
        var id = await InsertCourseAsync(_db, instructorId: instructorId, status: "published");
        await InsertLessonAsync(_db, id, position: 1);

        await using var context = NewContext();
        var queries = new CourseQueries(context);
        await queries.GetAsync(id, viewerId: null, cancellationToken);
        await queries.ListAsync(viewerId: null, afterId: null, take: 10, cancellationToken);
        await queries.ListPublishedAsync(after: null, take: 10, cancellationToken);
        await queries.ListByInstructorAsync(instructorId, cancellationToken);

        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task List_walks_every_course_in_id_order_one_page_at_a_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Guid[] ids =
        [
            await InsertCourseAsync(_db, status: "published"),
            await InsertCourseAsync(_db, status: "published"),
            await InsertCourseAsync(_db, status: "published"),
        ];

        await using var context = NewContext();
        var queries = new CourseQueries(context);
        var firstPage = await queries.ListAsync(viewerId: null, afterId: null, take: 2, cancellationToken);
        var lastPage = await queries.ListAsync(viewerId: null, firstPage[^1].Id, take: 2, cancellationToken);

        Assert.Equal(ids.Order().Take(2), firstPage.Select(course => course.Id));
        Assert.Equal(ids.Order().Skip(2), lastPage.Select(course => course.Id));
    }

    [Fact]
    public async Task List_returns_the_summary_of_each_course_with_its_status_name()
    {
        var instructorId = Guid.CreateVersion7();
        var id = await InsertCourseAsync(
            _db, instructorId: instructorId, title: "Clean Architecture", slug: "clean-architecture",
            priceAmount: 49.99m, priceCurrency: "EUR", status: "archived");

        await using var context = NewContext();
        var page = await new CourseQueries(context).ListAsync(instructorId, null, 10, TestContext.Current.CancellationToken);

        Assert.Equal(
            new CourseSummary(id, "Clean Architecture", "clean-architecture", 49.99m, "EUR", "Archived"),
            Assert.Single(page));
    }

    [Fact]
    public async Task Walking_the_public_catalog_returns_the_rows_of_the_versioned_listing_with_ties_on_a_page_border()
    {
        // With two per page, "SQL" spans the border between pages 1 and 2.
        var zeta = await InsertCourseAsync(_db, title: "Zeta", slug: "a-zeta", status: "published");
        await InsertLessonAsync(_db, zeta, position: 1, durationSeconds: 300);
        await InsertLessonAsync(_db, zeta, position: 2, durationSeconds: 600);
        await InsertCourseAsync(_db, title: "SQL", slug: "sql-c", status: "published");
        await InsertCourseAsync(_db, title: "SQL", slug: "sql-a", status: "published");
        var alpha = await InsertCourseAsync(_db, title: "Alpha", slug: "alpha", status: "published");
        await InsertLessonAsync(_db, alpha, position: 1, durationSeconds: 480);
        await InsertCourseAsync(_db, title: "SQL", slug: "sql-b", status: "published");
        await InsertCourseAsync(_db, title: "SQL", slug: "sql-draft", status: "draft");
        var archived = await InsertCourseAsync(_db, title: "Aardvark", slug: "aardvark", status: "archived");
        await InsertLessonAsync(_db, archived, position: 1);

        var pages = await WalkPublishedAsync(take: 2);

        Assert.Equal(
            [["alpha", "sql-a"], ["sql-b", "sql-c"], ["a-zeta"]],
            pages.Select(page => page.Select(course => course.Slug)));
        Assert.Equal(await VersionedListingAsync(), pages.SelectMany(page => page));
    }

    [Fact]
    public async Task Public_catalog_gives_a_course_without_lessons_zero_lessons_and_zero_seconds()
    {
        await InsertCourseAsync(_db, title: "Domain modeling", slug: "domain-modeling", status: "published");

        var pages = await WalkPublishedAsync(take: 10);

        var course = Assert.Single(Assert.Single(pages));
        Assert.Equal((0L, 0L), (course.LessonCount, course.TotalDurationSeconds));
    }

    [Fact]
    public async Task Totals_past_the_integer_range_stay_exact_like_the_bigint_of_the_versioned_listing()
    {
        var id = await InsertCourseAsync(_db, status: "published");
        await InsertLessonAsync(_db, id, position: 1, durationSeconds: 2_000_000_000);
        await InsertLessonAsync(_db, id, position: 2, durationSeconds: 2_000_000_000);

        var pages = await WalkPublishedAsync(take: 10);

        Assert.Equal(4_000_000_000L, Assert.Single(Assert.Single(pages)).TotalDurationSeconds);
        Assert.Equal(await VersionedListingAsync(), pages.SelectMany(page => page));
    }

    [Fact]
    public async Task Instructor_listing_returns_the_rows_of_the_versioned_listing_in_every_status()
    {
        var instructorId = Guid.CreateVersion7();
        var published = await InsertCourseAsync(
            _db, instructorId: instructorId, title: "Testing", slug: "testing-b", status: "published");
        await InsertLessonAsync(_db, published, position: 1, durationSeconds: 300);
        await InsertLessonAsync(_db, published, position: 2, durationSeconds: 600);
        await InsertCourseAsync(_db, instructorId: instructorId, title: "Testing", slug: "testing-a", status: "draft");
        await InsertCourseAsync(_db, instructorId: instructorId, title: "Legacy", slug: "legacy", status: "archived");
        var other = await InsertCourseAsync(_db, title: "Another instructor", status: "published");
        await InsertLessonAsync(_db, other, position: 1);

        await using var context = NewContext();
        var listing = await new CourseQueries(context)
            .ListByInstructorAsync(instructorId, TestContext.Current.CancellationToken);

        Assert.Equal(["Archived", "Draft", "Published"], listing.Select(course => course.Status));
        Assert.Equal(
            await VersionedInstructorListingAsync(instructorId),
            listing.Select(course => course with { Status = course.Status.ToLowerInvariant() }));
    }

    [Fact]
    public async Task Listings_send_one_command_however_many_courses_they_return()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var instructorId = Guid.CreateVersion7();
        for (var number = 0; number < 5; number++)
        {
            var id = await InsertCourseAsync(_db, instructorId: instructorId, status: "published");
            await InsertLessonAsync(_db, id, position: 1);
        }

        await using var context = NewContext();
        var queries = new CourseQueries(context);

        Assert.Equal(5, (await queries.ListPublishedAsync(after: null, take: 10, cancellationToken)).Count);
        Assert.Single(_recorder.Commands);

        _recorder.Clear();
        Assert.Equal(5, (await queries.ListByInstructorAsync(instructorId, cancellationToken)).Count);
        Assert.Single(_recorder.Commands);
    }

    private async Task<List<List<PublishedCourse>>> WalkPublishedAsync(int take)
    {
        await using var context = NewContext();
        var queries = new CourseQueries(context);
        var pages = new List<List<PublishedCourse>>();
        PublishedCourseCursor? after = null;
        while (true)
        {
            var page = await queries.ListPublishedAsync(after, take, TestContext.Current.CancellationToken);
            if (page.Count == 0)
            {
                return pages;
            }

            pages.Add([.. page]);
            Assert.True(pages.Count <= 100, "Pagination does not end.");
            after = new(CourseTitle.Create(page[^1].Title).Value, page[^1].Slug);
        }
    }

    private async Task<List<PublishedCourse>> VersionedListingAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync("list_published_courses.sql"));
        await using var row = await command.ExecuteReaderAsync(cancellationToken);

        var courses = new List<PublishedCourse>();
        while (await row.ReadAsync(cancellationToken))
        {
            courses.Add(new(
                row.GetGuid(row.GetOrdinal("id")),
                row.GetString(row.GetOrdinal("title")),
                row.GetString(row.GetOrdinal("slug")),
                row.GetDecimal(row.GetOrdinal("price_amount")),
                row.GetString(row.GetOrdinal("price_currency")),
                row.GetInt64(row.GetOrdinal("lesson_count")),
                row.GetInt64(row.GetOrdinal("total_duration_seconds"))));
        }

        return courses;
    }

    private async Task<List<InstructorCourse>> VersionedInstructorListingAsync(Guid instructorId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync("list_instructor_courses.sql"));
        command.Parameters.AddWithValue(instructorId);
        await using var row = await command.ExecuteReaderAsync(cancellationToken);

        var courses = new List<InstructorCourse>();
        while (await row.ReadAsync(cancellationToken))
        {
            courses.Add(new(
                row.GetGuid(row.GetOrdinal("id")),
                row.GetString(row.GetOrdinal("title")),
                row.GetString(row.GetOrdinal("slug")),
                row.GetString(row.GetOrdinal("status")),
                row.GetDecimal(row.GetOrdinal("price_amount")),
                row.GetString(row.GetOrdinal("price_currency")),
                row.GetInt64(row.GetOrdinal("lesson_count")),
                row.GetInt64(row.GetOrdinal("total_duration_seconds"))));
        }

        return courses;
    }

    private async Task<uint> ReadVersionAsync(Guid id)
    {
        await using var command = _db.CreateCommand("SELECT xmin FROM courses WHERE id = $1");
        command.Parameters.AddWithValue(id);

        return (uint)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private CatalogDbContext NewContext() => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_db)
            .AddInterceptors(_recorder)
            .Options);
}
