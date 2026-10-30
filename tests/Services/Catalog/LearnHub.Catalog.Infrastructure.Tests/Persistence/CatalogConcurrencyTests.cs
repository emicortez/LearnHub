using Npgsql;

using static LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogRows;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Interleaves two real connections step by step to pin what PostgreSQL does under concurrency: which anomalies each
/// isolation level lets through and which correction stops them. Each step is awaited, so the order is the test's, not
/// the scheduler's; a session expected to wait is proved blocked through pg_blocking_pids, never through a delay.
/// The write operations the EF Core repository adds in module 6 must choose among these corrections.
/// </summary>
public sealed class CatalogConcurrencyTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private const string ReadPrice = "SELECT price_amount FROM courses WHERE id = $1";
    private const string ReadPriceForUpdate = "SELECT price_amount FROM courses WHERE id = $1 FOR UPDATE";
    private const string SetPrice = "UPDATE courses SET price_amount = $1 WHERE id = $2";
    private const string AddToPrice = "UPDATE courses SET price_amount = price_amount + $1 WHERE id = $2";
    private const string CountDrafts = "SELECT count(*) FROM courses WHERE instructor_id = $1 AND status = 'draft'";
    private const string MakeDraft = "UPDATE courses SET status = 'draft' WHERE id = $1";
    private const string ReadCommitted = "BEGIN ISOLATION LEVEL READ COMMITTED";
    private const string RepeatableRead = "BEGIN ISOLATION LEVEL REPEATABLE READ";
    private const string Serializable = "BEGIN ISOLATION LEVEL SERIALIZABLE";
    private const string SerializationFailure = "40001";
    private const string UniqueViolation = "23505";
    private const int MaxDraftsPerInstructor = 3;

    private static readonly TimeSpan BlockedTimeout = TimeSpan.FromSeconds(10);

    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Read_committed_loses_the_first_update_of_a_read_calculate_write_without_any_error()
    {
        var course = await InsertCourseAsync(_db, priceAmount: 49.99m);
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(ReadCommitted);
        await b.ExecuteAsync(ReadCommitted);

        var readByA = await a.ScalarAsync<decimal>(ReadPrice, course);
        var readByB = await b.ScalarAsync<decimal>(ReadPrice, course);
        await a.ExecuteAsync(SetPrice, readByA + 10m, course);
        await a.ExecuteAsync("COMMIT");
        await b.ExecuteAsync(SetPrice, readByB + 5m, course);
        await b.ExecuteAsync("COMMIT");

        Assert.Equal(54.99m, await PriceAsync(course));
    }

    [Fact]
    public async Task Atomic_update_waits_for_the_first_writer_and_keeps_both_changes()
    {
        var course = await InsertCourseAsync(_db, priceAmount: 49.99m);
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(ReadCommitted);
        await a.ExecuteAsync(AddToPrice, 10m, course);

        var secondUpdate = b.ExecuteAsync(AddToPrice, 5m, course);
        await WaitUntilBlockedAsync(b);
        await a.ExecuteAsync("COMMIT");
        await secondUpdate;

        Assert.Equal(64.99m, await PriceAsync(course));
    }

    [Fact]
    public async Task Select_for_update_makes_the_second_session_wait_and_read_the_first_write()
    {
        var course = await InsertCourseAsync(_db, priceAmount: 49.99m);
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(ReadCommitted);
        await b.ExecuteAsync(ReadCommitted);

        var readByA = await a.ScalarAsync<decimal>(ReadPriceForUpdate, course);
        var lockByB = b.ScalarAsync<decimal>(ReadPriceForUpdate, course);
        await WaitUntilBlockedAsync(b);
        await a.ExecuteAsync(SetPrice, readByA + 10m, course);
        await a.ExecuteAsync("COMMIT");
        var readByB = await lockByB;
        await b.ExecuteAsync(SetPrice, readByB + 5m, course);
        await b.ExecuteAsync("COMMIT");

        Assert.Equal(59.99m, readByB);
        Assert.Equal(64.99m, await PriceAsync(course));
    }

    [Fact]
    public async Task Repeatable_read_rejects_the_second_writer_and_only_a_full_retry_keeps_both_changes()
    {
        var course = await InsertCourseAsync(_db, priceAmount: 49.99m);
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(RepeatableRead);
        await b.ExecuteAsync(RepeatableRead);

        var readByA = await a.ScalarAsync<decimal>(ReadPrice, course);
        var readByB = await b.ScalarAsync<decimal>(ReadPrice, course);
        await a.ExecuteAsync(SetPrice, readByA + 10m, course);
        await a.ExecuteAsync("COMMIT");

        var failure = await Assert.ThrowsAsync<PostgresException>(() => b.ExecuteAsync(SetPrice, readByB + 5m, course));
        Assert.Equal(SerializationFailure, failure.SqlState);
        await b.ExecuteAsync("ROLLBACK");
        Assert.Equal(59.99m, await PriceAsync(course));

        // The retry starts over: a new snapshot, a new read, a new decision.
        await b.ExecuteAsync(RepeatableRead);
        var reread = await b.ScalarAsync<decimal>(ReadPrice, course);
        await b.ExecuteAsync(SetPrice, reread + 5m, course);
        await b.ExecuteAsync("COMMIT");

        Assert.Equal(64.99m, await PriceAsync(course));
    }

    [Fact]
    public async Task Repeatable_read_takes_its_snapshot_at_the_first_statement_not_at_begin()
    {
        var course = await InsertCourseAsync(_db, priceAmount: 49.99m);
        await using var a = await OpenSessionAsync();
        await a.ExecuteAsync(RepeatableRead);

        await SetPriceOutsideAsync(course, 59.99m);
        var firstRead = await a.ScalarAsync<decimal>(ReadPrice, course);
        await SetPriceOutsideAsync(course, 69.99m);
        var secondRead = await a.ScalarAsync<decimal>(ReadPrice, course);

        Assert.Equal(59.99m, firstRead);
        Assert.Equal(59.99m, secondRead);
    }

    [Fact]
    public async Task Repeatable_read_lets_a_write_skew_break_a_rule_across_rows()
    {
        var (instructor, first, second) = await SeedTwoDraftsAndTwoArchivedCoursesAsync();
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(RepeatableRead);
        await b.ExecuteAsync(RepeatableRead);

        Assert.True(await a.ScalarAsync<long>(CountDrafts, instructor) < MaxDraftsPerInstructor);
        Assert.True(await b.ScalarAsync<long>(CountDrafts, instructor) < MaxDraftsPerInstructor);
        await a.ExecuteAsync(MakeDraft, first);
        await b.ExecuteAsync(MakeDraft, second);
        await a.ExecuteAsync("COMMIT");
        await b.ExecuteAsync("COMMIT");

        Assert.Equal(4L, await DraftsAsync(instructor));
    }

    [Fact]
    public async Task Serializable_rejects_the_write_skew_and_the_retry_recounts_and_refuses()
    {
        var (instructor, first, second) = await SeedTwoDraftsAndTwoArchivedCoursesAsync();
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(Serializable);
        await b.ExecuteAsync(Serializable);

        Assert.True(await a.ScalarAsync<long>(CountDrafts, instructor) < MaxDraftsPerInstructor);
        Assert.True(await b.ScalarAsync<long>(CountDrafts, instructor) < MaxDraftsPerInstructor);

        // PostgreSQL chooses the victim and the statement that fails, so the test only pins that one of them fails.
        var aSurvived = await TryAsync(a, MakeDraft, first);
        var bSurvived = await TryAsync(b, MakeDraft, second);
        aSurvived = aSurvived && await TryAsync(a, "COMMIT");
        bSurvived = bSurvived && await TryAsync(b, "COMMIT");

        Assert.True(aSurvived != bSurvived, "Exactly one of the two transactions must fail with 40001.");
        Assert.Equal(MaxDraftsPerInstructor, await DraftsAsync(instructor));

        // The retry starts from the count: it now sees the limit reached, so it decides not to write.
        var loser = aSurvived ? b : a;
        await loser.ExecuteAsync(Serializable);
        Assert.Equal(MaxDraftsPerInstructor, await loser.ScalarAsync<long>(CountDrafts, instructor));
        await loser.ExecuteAsync("ROLLBACK");
    }

    [Fact]
    public async Task Reader_does_not_wait_for_an_uncommitted_writer_and_each_update_creates_a_new_row_version()
    {
        var course = await InsertCourseAsync(_db, priceAmount: 49.99m);
        await using var writer = await OpenSessionAsync();
        var versionBefore = await VersionAsync(course);
        await writer.ExecuteAsync(ReadCommitted);
        await writer.ExecuteAsync(SetPrice, 59.99m, course);

        // Would time out and fail if the reader waited for the writer.
        var whileUncommitted = await PriceAsync(course);
        var versionWhileUncommitted = await VersionAsync(course);
        await writer.ExecuteAsync("COMMIT");

        Assert.Equal(49.99m, whileUncommitted);
        Assert.Equal(versionBefore, versionWhileUncommitted);
        Assert.Equal(59.99m, await PriceAsync(course));
        Assert.NotEqual(versionBefore, await VersionAsync(course));
    }

    [Fact]
    public async Task Second_insert_at_the_same_position_waits_for_the_first_and_fails_when_it_commits()
    {
        var course = await InsertCourseAsync(_db);
        await using var a = await OpenSessionAsync();
        await using var b = await OpenSessionAsync();
        await a.ExecuteAsync(ReadCommitted);
        await InsertLessonAtPositionFourAsync(a, course);

        var secondInsert = InsertLessonAtPositionFourAsync(b, course);
        await WaitUntilBlockedAsync(b);
        await a.ExecuteAsync("COMMIT");
        var failure = await Assert.ThrowsAsync<PostgresException>(() => secondInsert);

        Assert.Equal(UniqueViolation, failure.SqlState);
        Assert.Equal("uq_lessons_course_position", failure.ConstraintName);
    }

    private static Task InsertLessonAtPositionFourAsync(Session session, Guid course) => session.ExecuteAsync(
        """
        INSERT INTO lessons (id, course_id, title, duration_seconds, position)
        VALUES ($1, $2, 'Lesson', 300, 4)
        """,
        Guid.CreateVersion7(),
        course);

    private static async Task<bool> TryAsync(Session session, string sql, params object[] parameters)
    {
        try
        {
            await session.ExecuteAsync(sql, parameters);
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == SerializationFailure)
        {
            await session.ExecuteAsync("ROLLBACK");
            return false;
        }
    }

    private async Task<(Guid Instructor, Guid First, Guid Second)> SeedTwoDraftsAndTwoArchivedCoursesAsync()
    {
        var instructor = Guid.CreateVersion7();
        await InsertCourseAsync(_db, instructorId: instructor);
        await InsertCourseAsync(_db, instructorId: instructor);
        var first = await InsertCourseAsync(_db, instructorId: instructor, status: "archived");
        var second = await InsertCourseAsync(_db, instructorId: instructor, status: "archived");
        return (instructor, first, second);
    }

    private async Task<Session> OpenSessionAsync() => new(await _db.OpenConnectionAsync());

    private async Task WaitUntilBlockedAsync(Session session)
    {
        await using var command = _db.CreateCommand("SELECT cardinality(pg_blocking_pids($1)) > 0");
        command.Parameters.AddWithValue(session.ProcessId);

        var deadline = DateTime.UtcNow + BlockedTimeout;
        while (!(bool)(await command.ExecuteScalarAsync())!)
        {
            Assert.True(DateTime.UtcNow < deadline, "The session never blocked.");
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
    }

    private async Task SetPriceOutsideAsync(Guid course, decimal price)
    {
        await using var outside = await OpenSessionAsync();
        await outside.ExecuteAsync(SetPrice, price, course);
    }

    private async Task<decimal> PriceAsync(Guid course)
    {
        await using var outside = await OpenSessionAsync();
        return await outside.ScalarAsync<decimal>(ReadPrice, course);
    }

    private async Task<string> VersionAsync(Guid course)
    {
        await using var outside = await OpenSessionAsync();
        return await outside.ScalarAsync<string>("SELECT xmin::text FROM courses WHERE id = $1", course);
    }

    private Task<long> DraftsAsync(Guid instructor) => CountAsync(_db, CountDrafts, instructor);

    /// <summary>One connection, so one PostgreSQL backend: the unit that holds a transaction and its locks.</summary>
    private sealed class Session(NpgsqlConnection connection) : IAsyncDisposable
    {
        public int ProcessId => connection.ProcessID;

        public async Task ExecuteAsync(string sql, params object[] parameters)
        {
            await using var command = Command(sql, parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<T> ScalarAsync<T>(string sql, params object[] parameters)
        {
            await using var command = Command(sql, parameters);
            return (T)(await command.ExecuteScalarAsync())!;
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();

        private NpgsqlCommand Command(string sql, object[] parameters)
        {
            // A session that hangs fails the test instead of the whole run.
            var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 10 };
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter);
            }

            return command;
        }
    }
}
