using Npgsql;

using static LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogRows;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

public sealed class CatalogSchemaTests(CatalogDatabaseFixture database)
{
    private readonly NpgsqlDataSource _db = database.DataSource;

    [Fact]
    public async Task Valid_course_with_lessons_is_stored_and_read_back_in_lesson_order()
    {
        var instructorId = Guid.CreateVersion7();
        var courseId = await InsertCourseAsync(_db, instructorId: instructorId, priceAmount: 10.50m, priceCurrency: "EUR");
        await InsertLessonAsync(_db, courseId, position: 2, title: "Second", durationSeconds: 300);
        await InsertLessonAsync(_db, courseId, position: 1, title: "First", durationSeconds: 120);

        await using (var course = _db.CreateCommand(
            "SELECT instructor_id, price_amount, price_currency, status FROM courses WHERE id = $1"))
        {
            course.Parameters.AddWithValue(courseId);
            await using var row = await course.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            Assert.True(await row.ReadAsync(TestContext.Current.CancellationToken));
            Assert.Equal(instructorId, row.GetGuid(0));
            Assert.Equal(10.50m, row.GetDecimal(1));
            Assert.Equal("EUR", row.GetString(2));
            Assert.Equal("draft", row.GetString(3));
        }

        await using var lessons = _db.CreateCommand(
            "SELECT title, duration_seconds FROM lessons WHERE course_id = $1 ORDER BY position");
        lessons.Parameters.AddWithValue(courseId);
        await using var rows = await lessons.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var stored = new List<(string Title, int Duration)>();
        while (await rows.ReadAsync(TestContext.Current.CancellationToken))
        {
            stored.Add((rows.GetString(0), rows.GetInt32(1)));
        }

        Assert.Equal([("First", 120), ("Second", 300)], stored);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("published")]
    [InlineData("archived")]
    public async Task Every_course_status_is_accepted(string status)
    {
        var courseId = await InsertCourseAsync(_db, status: status);

        Assert.Equal(1, await CountAsync(_db, "SELECT count(*) FROM courses WHERE id = $1", courseId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999999999.99)]
    public async Task Price_at_the_edges_of_the_range_is_accepted(decimal amount)
    {
        var courseId = await InsertCourseAsync(_db, priceAmount: amount);

        Assert.Equal(1, await CountAsync(_db, "SELECT count(*) FROM courses WHERE id = $1", courseId));
    }

    [Fact]
    public async Task Title_of_exactly_120_characters_is_accepted()
    {
        var courseId = await InsertCourseAsync(_db, title: new string('a', 120));

        Assert.Equal(1, await CountAsync(_db, "SELECT count(*) FROM courses WHERE id = $1", courseId));
    }

    [Theory]
    [InlineData(-0.01, "ck_courses_price_range")]
    [InlineData(10000000000, "ck_courses_price_range")]
    [InlineData(10.129, "ck_courses_price_scale")]
    public async Task Invalid_price_is_rejected_with_a_check_violation(decimal amount, string constraint)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, priceAmount: amount));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, constraint);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US1")]
    [InlineData("US")]
    public async Task Invalid_currency_is_rejected_with_a_check_violation(string currency)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, priceCurrency: currency));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_courses_currency");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_course_title_is_rejected_with_a_check_violation(string title)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, title: title));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_courses_title_length");
    }

    [Fact]
    public async Task Course_title_longer_than_120_characters_is_rejected_with_a_check_violation()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, title: new string('a', 121)));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_courses_title_length");
    }

    [Fact]
    public async Task Blank_slug_is_rejected_with_a_check_violation()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, slug: " "));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_courses_slug_not_blank");
    }

    [Fact]
    public async Task Empty_instructor_id_is_rejected_with_a_check_violation()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, instructorId: Guid.Empty));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_courses_instructor_id_not_empty");
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("deleted")]
    public async Task Unknown_status_is_rejected_with_a_check_violation(string status)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, status: status));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_courses_status");
    }

    [Fact]
    public async Task Duplicate_slug_is_rejected_with_a_unique_violation()
    {
        var slug = $"duplicate-{Guid.CreateVersion7():N}";
        await InsertCourseAsync(_db, slug: slug);

        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, slug: slug));

        AssertViolation(error, PostgresErrorCodes.UniqueViolation, "uq_courses_slug");
    }

    [Fact]
    public async Task Duplicate_course_id_is_rejected_with_a_unique_violation()
    {
        var courseId = await InsertCourseAsync(_db);

        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, id: courseId));

        AssertViolation(error, PostgresErrorCodes.UniqueViolation, "pk_courses");
    }

    [Fact]
    public async Task Lesson_for_a_missing_course_is_rejected_with_a_foreign_key_violation()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => InsertLessonAsync(_db, courseId: Guid.CreateVersion7(), position: 1));

        AssertViolation(error, PostgresErrorCodes.ForeignKeyViolation, "fk_lessons_course");
    }

    [Fact]
    public async Task Duplicate_lesson_position_in_the_same_course_is_rejected_with_a_unique_violation()
    {
        var courseId = await InsertCourseAsync(_db);
        await InsertLessonAsync(_db, courseId, position: 1);

        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertLessonAsync(_db, courseId, position: 1));

        AssertViolation(error, PostgresErrorCodes.UniqueViolation, "uq_lessons_course_position");
    }

    [Fact]
    public async Task Same_lesson_position_in_different_courses_is_accepted()
    {
        var first = await InsertCourseAsync(_db);
        var second = await InsertCourseAsync(_db);

        await InsertLessonAsync(_db, first, position: 1);
        await InsertLessonAsync(_db, second, position: 1);

        Assert.Equal(1, await CountAsync(_db, "SELECT count(*) FROM lessons WHERE course_id = $1", second));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Lesson_position_below_one_is_rejected_with_a_check_violation(int position)
    {
        var courseId = await InsertCourseAsync(_db);

        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertLessonAsync(_db, courseId, position));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_lessons_position_positive");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public async Task Lesson_duration_that_is_not_positive_is_rejected_with_a_check_violation(int seconds)
    {
        var courseId = await InsertCourseAsync(_db);

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => InsertLessonAsync(_db, courseId, position: 1, durationSeconds: seconds));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_lessons_duration_positive");
    }

    [Fact]
    public async Task Blank_lesson_title_is_rejected_with_a_check_violation()
    {
        var courseId = await InsertCourseAsync(_db);

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => InsertLessonAsync(_db, courseId, position: 1, title: "  "));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_lessons_title_not_blank");
    }

    [Fact]
    public async Task Deleting_a_course_deletes_its_lessons()
    {
        var courseId = await InsertCourseAsync(_db);
        await InsertLessonAsync(_db, courseId, position: 1);
        await InsertLessonAsync(_db, courseId, position: 2);

        await using (var delete = _db.CreateCommand("DELETE FROM courses WHERE id = $1"))
        {
            delete.Parameters.AddWithValue(courseId);
            await delete.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, await CountAsync(_db, "SELECT count(*) FROM lessons WHERE course_id = $1", courseId));
    }

    [Fact]
    public async Task Rejected_insert_stores_nothing()
    {
        var courseId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<PostgresException>(() => InsertCourseAsync(_db, id: courseId, priceAmount: -1m));

        Assert.Equal(0, await CountAsync(_db, "SELECT count(*) FROM courses WHERE id = $1", courseId));
    }

    private static void AssertViolation(PostgresException error, string sqlState, string constraint)
    {
        Assert.Equal(sqlState, error.SqlState);
        Assert.Equal(constraint, error.ConstraintName);
    }
}
