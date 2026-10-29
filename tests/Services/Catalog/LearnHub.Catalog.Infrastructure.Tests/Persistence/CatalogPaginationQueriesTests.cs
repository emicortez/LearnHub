using Npgsql;

using static LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogRows;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// The keyset-paginated public listing, each test on a database of its own: walking every page must give
/// exactly the rows of the unpaginated listing, in its order.
/// </summary>
public sealed class CatalogPaginationQueriesTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private const int PageSize = 2;

    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Walking_every_page_returns_each_published_course_once_in_listing_order_even_with_ties_on_a_page_border()
    {
        // Inserted out of order. With two per page, "SQL" spans the border between pages 1 and 2.
        await InsertPublishedAsync("Zeta", "zeta");
        await InsertPublishedAsync("SQL", "sql-c");
        await InsertPublishedAsync("SQL", "sql-a");
        await InsertPublishedAsync("Alpha", "alpha");
        await InsertPublishedAsync("SQL", "sql-b");
        await InsertCourseAsync(_db, title: "SQL", slug: "sql-draft", status: "draft");
        await InsertCourseAsync(_db, title: "Aardvark", slug: "aardvark-archived", status: "archived");

        var pages = await WalkAllPagesAsync();

        Assert.Equal(
            [["alpha", "sql-a"], ["sql-b", "sql-c"], ["zeta"]],
            pages.Select(page => page.Select(course => course.Slug)));
        Assert.Equal(await UnpaginatedListingAsync(), pages.SelectMany(page => page));
    }

    [Fact]
    public async Task Pages_keep_the_lesson_totals_including_zero_for_a_course_without_lessons()
    {
        var withLessons = await InsertPublishedAsync("Alpha", "alpha");
        await InsertLessonAsync(_db, withLessons, position: 1, durationSeconds: 300);
        await InsertLessonAsync(_db, withLessons, position: 2, durationSeconds: 600);
        await InsertPublishedAsync("Beta", "beta");
        var third = await InsertPublishedAsync("Gamma", "gamma");
        await InsertLessonAsync(_db, third, position: 1, durationSeconds: 480);

        var pages = await WalkAllPagesAsync();

        Assert.Equal(
            [[("alpha", 2L, 900L), ("beta", 0L, 0L)], [("gamma", 1L, 480L)]],
            pages.Select(page => page.Select(course => (course.Slug, course.LessonCount, course.TotalDurationSeconds))));
    }

    [Fact]
    public async Task Page_after_the_last_row_is_empty()
    {
        await InsertPublishedAsync("Alpha", "alpha");
        await InsertPublishedAsync("Beta", "beta");

        var first = await FirstPageAsync(PageSize);

        Assert.Equal(["alpha", "beta"], first.Select(course => course.Slug));
        Assert.Empty(await NextPageAsync(first[^1], PageSize));
    }

    [Fact]
    public async Task First_page_of_a_catalog_without_published_courses_is_empty()
    {
        await InsertCourseAsync(_db, slug: "only-draft", status: "draft");

        Assert.Empty(await FirstPageAsync(PageSize));
    }

    private Task<Guid> InsertPublishedAsync(string title, string slug) =>
        InsertCourseAsync(_db, title: title, slug: slug, status: "published");

    private async Task<List<List<ListedCourse>>> WalkAllPagesAsync()
    {
        var pages = new List<List<ListedCourse>>();
        var page = await FirstPageAsync(PageSize);
        while (page.Count > 0)
        {
            pages.Add(page);
            Assert.True(pages.Count <= 100, "Pagination does not end.");
            // Always asks for the page after a short one too: it must come back empty.
            page = await NextPageAsync(page[^1], PageSize);
        }

        return pages;
    }

    private async Task<List<ListedCourse>> FirstPageAsync(int pageSize)
    {
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync("list_published_courses_first_page.sql"));
        command.Parameters.AddWithValue(pageSize);
        return await ReadCoursesAsync(command);
    }

    private async Task<List<ListedCourse>> NextPageAsync(ListedCourse lastOfPreviousPage, int pageSize)
    {
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync("list_published_courses_next_page.sql"));
        command.Parameters.AddWithValue(lastOfPreviousPage.Title);
        command.Parameters.AddWithValue(lastOfPreviousPage.Slug);
        command.Parameters.AddWithValue(pageSize);
        return await ReadCoursesAsync(command);
    }

    private async Task<List<ListedCourse>> UnpaginatedListingAsync()
    {
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync("list_published_courses.sql"));
        return await ReadCoursesAsync(command);
    }

    private static async Task<List<ListedCourse>> ReadCoursesAsync(NpgsqlCommand command)
    {
        await using var row = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var courses = new List<ListedCourse>();
        while (await row.ReadAsync(TestContext.Current.CancellationToken))
        {
            courses.Add(new ListedCourse(
                Id: row.GetGuid(row.GetOrdinal("id")),
                Title: row.GetString(row.GetOrdinal("title")),
                Slug: row.GetString(row.GetOrdinal("slug")),
                PriceAmount: row.GetDecimal(row.GetOrdinal("price_amount")),
                PriceCurrency: row.GetString(row.GetOrdinal("price_currency")),
                LessonCount: row.GetInt64(row.GetOrdinal("lesson_count")),
                TotalDurationSeconds: row.GetInt64(row.GetOrdinal("total_duration_seconds"))));
        }

        return courses;
    }

    private sealed record ListedCourse(
        Guid Id,
        string Title,
        string Slug,
        decimal PriceAmount,
        string PriceCurrency,
        long LessonCount,
        long TotalDurationSeconds);
}
