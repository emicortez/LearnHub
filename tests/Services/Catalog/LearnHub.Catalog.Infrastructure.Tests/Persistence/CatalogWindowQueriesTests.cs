using Npgsql;

using static LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogRows;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// The window-function queries (course outline, longest courses per instructor), each on a database of its own.
/// </summary>
public sealed class CatalogWindowQueriesTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Outline_lists_lessons_in_position_order_with_start_offsets_percentages_and_course_total()
    {
        var courseId = await InsertCourseAsync(_db, status: "published");
        // Inserted out of order: the outline must follow position, not insertion.
        await InsertLessonAsync(_db, courseId, position: 3, title: "Normalization", durationSeconds: 900);
        await InsertLessonAsync(_db, courseId, position: 1, title: "Introduction", durationSeconds: 300);
        await InsertLessonAsync(_db, courseId, position: 2, title: "Constraints", durationSeconds: 600);

        var outline = await OutlineAsync(courseId);

        Assert.Equal(
            [
                new OutlineLesson(1, "Introduction", 300, 0, 16.7m, 1800),
                new OutlineLesson(2, "Constraints", 600, 300, 33.3m, 1800),
                new OutlineLesson(3, "Normalization", 900, 900, 50.0m, 1800),
            ],
            outline);
    }

    [Fact]
    public async Task Outline_of_a_single_lesson_course_starts_at_zero_and_is_the_whole_course()
    {
        var courseId = await InsertCourseAsync(_db, status: "published");
        await InsertLessonAsync(_db, courseId, position: 1, title: "Layers", durationSeconds: 480);

        Assert.Equal([new OutlineLesson(1, "Layers", 480, 0, 100.0m, 480)], await OutlineAsync(courseId));
    }

    [Fact]
    public async Task Outline_percentage_keeps_decimals_instead_of_truncating()
    {
        var courseId = await InsertCourseAsync(_db, status: "published");
        await InsertLessonAsync(_db, courseId, position: 1, durationSeconds: 100);
        await InsertLessonAsync(_db, courseId, position: 2, durationSeconds: 200);

        // Integer division would give 33 and 66.
        Assert.Equal([33.3m, 66.7m], (await OutlineAsync(courseId)).Select(lesson => lesson.PercentageOfCourse));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("archived")]
    public async Task Outline_of_an_unpublished_course_is_empty(string status)
    {
        var courseId = await InsertCourseAsync(_db, status: status);
        await InsertLessonAsync(_db, courseId, position: 1);

        Assert.Empty(await OutlineAsync(courseId));
    }

    [Fact]
    public async Task Outline_of_a_course_without_lessons_or_an_unknown_course_is_empty()
    {
        var emptyCourseId = await InsertCourseAsync(_db, status: "published");

        Assert.Empty(await OutlineAsync(emptyCourseId));
        Assert.Empty(await OutlineAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task Outline_shows_only_the_requested_course()
    {
        var courseId = await InsertCourseAsync(_db, status: "published");
        await InsertLessonAsync(_db, courseId, position: 1, title: "Mine", durationSeconds: 300);
        var otherId = await InsertCourseAsync(_db, status: "published");
        await InsertLessonAsync(_db, otherId, position: 1, title: "Other", durationSeconds: 600);

        Assert.Equal([new OutlineLesson(1, "Mine", 300, 0, 100.0m, 300)], await OutlineAsync(courseId));
    }

    [Fact]
    public async Task Ranking_has_at_most_three_published_courses_per_instructor_longest_first_ties_broken_by_slug()
    {
        var instructorA = Guid.CreateVersion7();
        var instructorB = Guid.CreateVersion7();
        await InsertCourseWithLessonsAsync(instructorA, "a-longest", "published", 1800);
        await InsertCourseWithLessonsAsync(instructorA, "a-second", "published", 600, 600);
        // Tied for third place: the slug decides, and only one of them makes the cut.
        await InsertCourseWithLessonsAsync(instructorA, "a-tied-z", "published", 900);
        await InsertCourseWithLessonsAsync(instructorA, "a-tied-m", "published", 900);
        await InsertCourseWithLessonsAsync(instructorA, "a-draft", "draft", 5000);
        await InsertCourseWithLessonsAsync(instructorA, "a-archived", "archived", 5000);
        await InsertCourseWithLessonsAsync(instructorB, "b-empty", "published");

        var ranking = await RankingAsync();

        Assert.Equal(
            [
                new RankedCourse(instructorA, "a-longest", 1800, 1),
                new RankedCourse(instructorA, "a-second", 1200, 2),
                new RankedCourse(instructorA, "a-tied-m", 900, 3),
                new RankedCourse(instructorB, "b-empty", 0, 1),
            ],
            ranking.OrderBy(course => course.InstructorId == instructorA ? 0 : 1).ThenBy(course => course.Rank));
    }

    [Fact]
    public async Task Ranking_is_ordered_by_instructor_then_rank()
    {
        var first = new Guid("00000000-0000-0000-0000-000000000001");
        var second = new Guid("00000000-0000-0000-0000-000000000002");
        await InsertCourseWithLessonsAsync(second, "second-only", "published", 300);
        await InsertCourseWithLessonsAsync(first, "first-short", "published", 300);
        await InsertCourseWithLessonsAsync(first, "first-long", "published", 900);

        Assert.Equal(
            [(first, "first-long", 1L), (first, "first-short", 2L), (second, "second-only", 1L)],
            (await RankingAsync()).Select(course => (course.InstructorId, course.Slug, course.Rank)));
    }

    private async Task InsertCourseWithLessonsAsync(Guid instructorId, string slug, string status, params int[] durations)
    {
        var courseId = await InsertCourseAsync(_db, instructorId: instructorId, slug: slug, status: status);
        for (var index = 0; index < durations.Length; index++)
        {
            await InsertLessonAsync(_db, courseId, position: index + 1, durationSeconds: durations[index]);
        }
    }

    private async Task<List<OutlineLesson>> OutlineAsync(Guid courseId)
    {
        await using var command = _db.CreateCommand(await CatalogQueryFiles.ReadAsync("get_published_course_outline.sql"));
        command.Parameters.AddWithValue(courseId);

        await using var row = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var lessons = new List<OutlineLesson>();
        while (await row.ReadAsync(TestContext.Current.CancellationToken))
        {
            lessons.Add(new OutlineLesson(
                Position: row.GetInt32(row.GetOrdinal("position")),
                Title: row.GetString(row.GetOrdinal("title")),
                DurationSeconds: row.GetInt32(row.GetOrdinal("duration_seconds")),
                StartOffsetSeconds: row.GetInt64(row.GetOrdinal("start_offset_seconds")),
                PercentageOfCourse: row.GetDecimal(row.GetOrdinal("percentage_of_course")),
                TotalDurationSeconds: row.GetInt64(row.GetOrdinal("total_duration_seconds"))));
        }

        return lessons;
    }

    private async Task<List<RankedCourse>> RankingAsync()
    {
        await using var command = _db.CreateCommand(
            await CatalogQueryFiles.ReadAsync("list_longest_published_courses_per_instructor.sql"));

        await using var row = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var courses = new List<RankedCourse>();
        while (await row.ReadAsync(TestContext.Current.CancellationToken))
        {
            courses.Add(new RankedCourse(
                InstructorId: row.GetGuid(row.GetOrdinal("instructor_id")),
                Slug: row.GetString(row.GetOrdinal("slug")),
                TotalDurationSeconds: row.GetInt64(row.GetOrdinal("total_duration_seconds")),
                Rank: row.GetInt64(row.GetOrdinal("rank"))));
        }

        return courses;
    }

    private sealed record OutlineLesson(
        int Position,
        string Title,
        int DurationSeconds,
        long StartOffsetSeconds,
        decimal PercentageOfCourse,
        long TotalDurationSeconds);

    private sealed record RankedCourse(Guid InstructorId, string Slug, long TotalDurationSeconds, long Rank);
}
