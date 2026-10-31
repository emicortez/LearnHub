using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Pins how EF Core tracks and saves a <see cref="Course"/> against the real schema: a new context per unit of work,
/// as in a request, and the executed SQL read from the log, so "only what changed" is observed, not assumed.
/// </summary>
public sealed class CatalogDbContextTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private static readonly Guid InstructorId = Guid.CreateVersion7();

    private readonly List<string> _commands = [];

    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Saved_course_is_read_by_another_context_with_every_value()
    {
        var course = await SavePublishedCourseAsync();

        await using var context = NewContext();
        var loaded = await context.Courses.SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);

        Assert.NotSame(course, loaded);
        Assert.Equal(course.Id, loaded.Id);
        Assert.Equal(InstructorId, loaded.InstructorId);
        Assert.Equal(course.Title, loaded.Title);
        Assert.Equal(course.Slug, loaded.Slug);
        Assert.Equal(course.Price, loaded.Price);
        Assert.Equal(CourseStatus.Published, loaded.Status);
    }

    [Fact]
    public async Task New_course_goes_from_detached_to_added_to_unchanged()
    {
        var course = NewCourse();
        await using var context = NewContext();

        Assert.Equal(EntityState.Detached, context.Entry(course).State);

        context.Courses.Add(course);
        Assert.Equal(EntityState.Added, context.Entry(course).State);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EntityState.Unchanged, context.Entry(course).State);
    }

    [Fact]
    public async Task Loaded_course_needs_no_update_call_and_the_save_writes_only_what_changed()
    {
        var saved = await SavePublishedCourseAsync();
        await using var context = NewContext();
        var course = await context.Courses.SingleAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal(EntityState.Unchanged, context.Entry(course).State);

        Assert.True(course.Archive().IsSuccess);
        context.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Modified, context.Entry(course).State);

        _commands.Clear();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var update = Assert.Single(_commands);
        Assert.Contains("status", update, StringComparison.Ordinal);
        Assert.DoesNotContain("title", update, StringComparison.Ordinal);
        Assert.DoesNotContain("price_amount", update, StringComparison.Ordinal);
        Assert.Equal(EntityState.Unchanged, context.Entry(course).State);
        Assert.Equal(CourseStatus.Archived, await StoredStatusAsync(saved.Id));
    }

    [Fact]
    public async Task Calling_update_on_a_loaded_course_writes_every_column()
    {
        var saved = await SavePublishedCourseAsync();
        await using var context = NewContext();
        var course = await context.Courses.SingleAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);

        Assert.True(course.Archive().IsSuccess);
        context.Courses.Update(course);
        _commands.Clear();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Update walks the graph, so the course's lesson gets an UPDATE of its own too.
        var update = Assert.Single(_commands, command => command.Contains("UPDATE courses", StringComparison.Ordinal));
        Assert.Contains("status", update, StringComparison.Ordinal);
        Assert.Contains("title", update, StringComparison.Ordinal);
        Assert.Contains("price_amount", update, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Changed_price_is_saved_through_the_complex_property()
    {
        var saved = await SavePublishedCourseAsync();
        await using var context = NewContext();
        var course = await context.Courses.SingleAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);

        var newPrice = Money.Create(59.99m, "USD").Value;
        Assert.True(course.ChangePrice(newPrice).IsSuccess);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var reader = NewContext();
        var loaded = await reader.Courses.SingleAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal(newPrice, loaded.Price);
    }

    [Fact]
    public async Task Change_without_save_stays_in_memory()
    {
        var saved = await SavePublishedCourseAsync();
        await using var context = NewContext();
        var course = await context.Courses.SingleAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);

        Assert.True(course.Archive().IsSuccess);

        Assert.Equal(CourseStatus.Archived, course.Status);
        Assert.Equal(CourseStatus.Published, await StoredStatusAsync(saved.Id));
    }

    [Fact]
    public async Task Same_row_is_one_instance_but_each_query_goes_to_the_database()
    {
        var saved = await SavePublishedCourseAsync();
        await using var context = NewContext();
        _commands.Clear();

        var first = await context.Courses.FirstAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);
        var second = await context.Courses.FirstAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, _commands.Count);

        // ReferenceEquals, because == on an Entity compares ids and would pass for two instances.
        Assert.True(ReferenceEquals(first, second));

        var found = await context.Courses.FindAsync([saved.Id], TestContext.Current.CancellationToken);
        Assert.True(ReferenceEquals(first, found));
        Assert.Equal(2, _commands.Count);
    }

    [Fact]
    public async Task Second_query_does_not_overwrite_a_local_change()
    {
        var saved = await SavePublishedCourseAsync();
        await using var context = NewContext();
        var course = await context.Courses.FirstAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);
        Assert.True(course.Archive().IsSuccess);

        var again = await context.Courses.FirstAsync(c => c.Id == saved.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CourseStatus.Archived, again.Status);
    }

    [Fact]
    public async Task Saved_course_is_read_back_with_its_lessons_in_order()
    {
        var course = NewCourse();
        var first = course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).Value;
        var second = course.AddLesson("Use cases", TimeSpan.FromMinutes(90)).Value;
        await SaveAsync(course);

        await using var context = NewContext();
        var loaded = await context.CoursesWithOrderedLessons
            .SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);

        Assert.Equal([first.Id, second.Id], loaded.Lessons.Select(lesson => lesson.Id));
        Assert.Equal(["Dependency rule", "Use cases"], loaded.Lessons.Select(lesson => lesson.Title));
        Assert.Equal(TimeSpan.FromMinutes(100), loaded.TotalDuration);
    }

    [Fact]
    public async Task Lessons_are_stored_with_their_course_position_and_whole_seconds()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        Assert.True(course.AddLesson("Use cases", TimeSpan.FromSeconds(90)).IsSuccess);
        await SaveAsync(course);

        Assert.Equal([(course.Id, 600, 1), (course.Id, 90, 2)], await StoredLessonsAsync());
    }

    [Fact]
    public async Task Ordered_lessons_follow_the_stored_position_not_the_ids()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        Assert.True(course.AddLesson("Use cases", TimeSpan.FromMinutes(10)).IsSuccess);
        await SaveAsync(course);
        await ReversePositionsAgainstIdsAsync(course.Id);

        await using var context = NewContext();
        var loaded = await context.CoursesWithOrderedLessons
            .SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);

        Assert.Equal(await LessonIdsByPositionAsync(), loaded.Lessons.Select(lesson => lesson.Id));
    }

    [Fact]
    public async Task Plain_query_loads_the_whole_aggregate()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        await SaveAsync(course);

        await using var context = NewContext();
        var loaded = await context.Courses.SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);

        Assert.True(loaded.Publish().IsSuccess);
    }

    [Fact]
    public async Task Adding_a_lesson_to_a_loaded_course_inserts_one_row_at_the_next_position()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        Assert.True(course.AddLesson("Use cases", TimeSpan.FromMinutes(10)).IsSuccess);
        await SaveAsync(course);

        await using var context = NewContext();
        var loaded = await context.CoursesWithOrderedLessons
            .SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);
        Assert.True(loaded.AddLesson("Adapters", TimeSpan.FromMinutes(5)).IsSuccess);
        _commands.Clear();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var insert = Assert.Single(_commands);
        Assert.Contains("INSERT INTO lessons", insert, StringComparison.Ordinal);
        Assert.Equal([1, 2, 3], (await StoredLessonsAsync()).Select(lesson => lesson.Position));
    }

    [Fact]
    public async Task Saving_a_course_loaded_in_any_order_keeps_the_stored_positions()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        Assert.True(course.AddLesson("Use cases", TimeSpan.FromMinutes(10)).IsSuccess);
        await SaveAsync(course);
        await ReversePositionsAgainstIdsAsync(course.Id);
        var stored = await LessonIdsByPositionAsync();

        await using var context = NewContext();
        var loaded = await context.Courses.SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);
        Assert.True(loaded.ChangePrice(Money.Create(59.99m, "USD").Value).IsSuccess);
        _commands.Clear();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Single(_commands);
        Assert.Equal(stored, await LessonIdsByPositionAsync());
    }

    [Fact]
    public async Task Shadow_position_can_be_queried_and_read_from_the_tracker()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        var second = course.AddLesson("Use cases", TimeSpan.FromMinutes(10)).Value;
        await SaveAsync(course);

        await using var context = NewContext();
        var lesson = await context.Set<Lesson>()
            .SingleAsync(l => EF.Property<int>(l, "Position") == 2, TestContext.Current.CancellationToken);

        Assert.Equal(second.Id, lesson.Id);
        Assert.Equal(2, context.Entry(lesson).Property<int>("Position").CurrentValue);
    }

    [Fact]
    public async Task Deleting_a_course_deletes_its_lessons()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        await SaveAsync(course);

        await using var context = NewContext();
        var loaded = await context.Courses.SingleAsync(c => c.Id == course.Id, TestContext.Current.CancellationToken);
        context.Courses.Remove(loaded);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await StoredLessonsAsync());
    }

    [Fact]
    public async Task Duration_beyond_the_column_range_fails_instead_of_wrapping()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Too long", TimeSpan.FromSeconds(int.MaxValue + 1L)).IsSuccess);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(course));
        Assert.IsType<OverflowException>(exception.InnerException);
        Assert.Empty(await StoredLessonsAsync());
    }

    private static Course NewCourse() => Course.Create(
        InstructorId,
        CourseTitle.Create("Clean Architecture in .NET " + Guid.CreateVersion7()).Value,
        Money.Create(49.99m, "USD").Value);

    private static Course NewPublishedCourse()
    {
        var course = NewCourse();
        Assert.True(course.AddLesson("Dependency rule", TimeSpan.FromMinutes(10)).IsSuccess);
        Assert.True(course.Publish().IsSuccess);
        return course;
    }

    private async Task<Course> SavePublishedCourseAsync()
    {
        var course = NewPublishedCourse();
        await SaveAsync(course);
        return course;
    }

    private async Task SaveAsync(Course course)
    {
        await using var context = NewContext();
        context.Courses.Add(course);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<(Guid CourseId, int Seconds, int Position)>> StoredLessonsAsync()
    {
        await using var command = _db.CreateCommand(
            "SELECT course_id, duration_seconds, position FROM lessons ORDER BY position");
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var lessons = new List<(Guid, int, int)>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            lessons.Add((reader.GetGuid(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        return lessons;
    }

    // Lessons created in the same millisecond have ids in no fixed order, so the positions are set against the ids in
    // SQL: an order that comes from the ids, and not from the position, cannot pass by luck. Moved out of the way first,
    // because uq_lessons_course_position is checked after each statement.
    private async Task ReversePositionsAgainstIdsAsync(Guid courseId)
    {
        await using var batch = _db.CreateBatch();
        foreach (var sql in new[]
        {
            "UPDATE lessons SET position = position + 100 WHERE course_id = $1",
            """
            UPDATE lessons AS l
            SET position = ranked.position
            FROM (SELECT id, row_number() OVER (ORDER BY id DESC) AS position FROM lessons WHERE course_id = $1) AS ranked
            WHERE l.id = ranked.id
            """,
        })
        {
            var command = batch.CreateBatchCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue(courseId);
            batch.BatchCommands.Add(command);
        }

        await batch.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<Guid>> LessonIdsByPositionAsync()
    {
        await using var command = _db.CreateCommand("SELECT id FROM lessons ORDER BY position");
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var ids = new List<Guid>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private async Task<CourseStatus> StoredStatusAsync(Guid id)
    {
        await using var reader = NewContext();
        return await reader.Courses
            .Where(course => course.Id == id)
            .Select(course => course.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private CatalogDbContext NewContext() => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_db)
            .LogTo(_commands.Add, [RelationalEventId.CommandExecuted])
            .Options);
}
