using LearnHub.Catalog.Application;
using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;
using LearnHub.SharedKernel;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Pins what the repository and the unit of work promise the handlers, on a real database of its own for each test.
/// </summary>
public sealed class CatalogUnitOfWorkTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Adding_to_the_repository_stores_nothing_until_the_unit_of_work_commits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = NewContext();
        var course = NewCourse("Pending Course");

        new CourseRepository(context).Add(course);

        Assert.False(await ExistsAsync(course.Id));

        var saved = await new CatalogUnitOfWork(context, new RecordingDispatcher()).SaveChangesAsync(cancellationToken);

        Assert.True(saved.IsSuccess);
        Assert.True(await ExistsAsync(course.Id));
    }

    [Fact]
    public async Task Get_returns_null_for_an_unknown_course()
    {
        await using var context = NewContext();

        var course = await new CourseRepository(context).GetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(course);
    }

    [Fact]
    public async Task Get_returns_the_whole_aggregate_with_its_lessons_in_position_order()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Ordered Course");
        Assert.True(course.AddLesson("One", TimeSpan.FromMinutes(1)).IsSuccess);
        Assert.True(course.AddLesson("Two", TimeSpan.FromMinutes(2)).IsSuccess);
        await SaveAsync(course);

        // Position runs against id, so a read that followed the key instead of the position would fail.
        await using (var reverse = _db.CreateCommand(
            """
            UPDATE lessons AS l
            SET position = 100 - r.n
            FROM (SELECT id, row_number() OVER (ORDER BY id) AS n FROM lessons) AS r
            WHERE l.id = r.id
            """))
        {
            await reverse.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var context = NewContext();
        var loaded = await new CourseRepository(context).GetAsync(course.Id, cancellationToken);

        Assert.Equal(
            course.Lessons.Select(lesson => lesson.Id).OrderDescending(),
            loaded!.Lessons.Select(lesson => lesson.Id));
    }

    [Fact]
    public async Task List_walks_every_course_in_id_order_one_page_at_a_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ids = new List<Guid>();
        foreach (var title in new[] { "First", "Second", "Third" })
        {
            var course = NewCourse(title);
            await SaveAsync(course);
            ids.Add(course.Id);
        }

        await using var context = NewContext();
        var repository = new CourseRepository(context);
        var firstPage = await repository.ListAsync(afterId: null, take: 2, cancellationToken);
        var lastPage = await repository.ListAsync(firstPage[^1].Id, take: 2, cancellationToken);

        Assert.Equal(ids.Order().Take(2), firstPage.Select(course => course.Id));
        Assert.Equal(ids.Order().Skip(2), lastPage.Select(course => course.Id));
    }

    [Fact]
    public async Task Committed_events_are_dispatched_after_the_commit_and_cleared()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Published Course");
        Assert.True(course.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        Assert.True(course.Publish().IsSuccess);
        var committedAtDispatch = false;
        var dispatcher = new RecordingDispatcher(async () => committedAtDispatch = await ExistsAsync(course.Id));
        await using var context = NewContext();
        var unitOfWork = new CatalogUnitOfWork(context, dispatcher);
        new CourseRepository(context).Add(course);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        Assert.True(committedAtDispatch);
        Assert.Single(dispatcher.Events.OfType<CoursePublished>());
        Assert.Empty(course.DomainEvents);

        var dispatched = dispatcher.Events.Count;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Assert.Equal(dispatched, dispatcher.Events.Count);
    }

    [Fact]
    public async Task A_taken_slug_is_a_failure_and_dispatches_nothing()
    {
        await SaveAsync(NewCourse("Taken Slug"));
        var course = NewCourse("Taken Slug");
        Assert.True(course.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        Assert.True(course.Publish().IsSuccess);
        var dispatcher = new RecordingDispatcher();
        await using var context = NewContext();
        new CourseRepository(context).Add(course);

        var saved = await new CatalogUnitOfWork(context, dispatcher).SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(saved.IsFailure);
        Assert.Equal(CourseErrors.SlugTaken, saved.Error);
        Assert.Empty(dispatcher.Events);
        Assert.False(await ExistsAsync(course.Id));
    }

    [Fact]
    public async Task Another_unique_violation_is_not_reported_as_a_taken_slug()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Racing Lessons");
        await SaveAsync(course);

        await using var first = NewContext();
        await using var second = NewContext();
        var firstCopy = await new CourseRepository(first).GetAsync(course.Id, cancellationToken);
        var secondCopy = await new CourseRepository(second).GetAsync(course.Id, cancellationToken);
        Assert.True(firstCopy!.AddLesson("One", TimeSpan.FromMinutes(1)).IsSuccess);
        Assert.True(secondCopy!.AddLesson("Two", TimeSpan.FromMinutes(1)).IsSuccess);
        await new CatalogUnitOfWork(first, new RecordingDispatcher()).SaveChangesAsync(cancellationToken);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => new CatalogUnitOfWork(second, new RecordingDispatcher()).SaveChangesAsync(cancellationToken));

        var violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("uq_lessons_course_position", violation.ConstraintName);
    }

    [Fact]
    public async Task A_row_changed_since_it_was_loaded_is_a_failure_and_dispatches_nothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Stale Course");
        Assert.True(course.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        await SaveAsync(course);

        await using var first = NewContext();
        await using var second = NewContext();
        var firstCopy = await new CourseRepository(first).GetAsync(course.Id, cancellationToken);
        var secondCopy = await new CourseRepository(second).GetAsync(course.Id, cancellationToken);
        Assert.True(firstCopy!.Publish().IsSuccess);
        Assert.True(secondCopy!.Publish().IsSuccess);
        await new CatalogUnitOfWork(first, new RecordingDispatcher()).SaveChangesAsync(cancellationToken);
        var dispatcher = new RecordingDispatcher();

        var saved = await new CatalogUnitOfWork(second, dispatcher).SaveChangesAsync(cancellationToken);

        Assert.True(saved.IsFailure);
        Assert.Equal(CourseErrors.VersionMismatch, saved.Error);
        Assert.Empty(dispatcher.Events);
        Assert.NotEmpty(secondCopy.DomainEvents);
    }

    [Fact]
    public async Task The_version_changes_when_the_course_is_written()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Versioned Course");
        Assert.True(course.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        await using var context = NewContext();
        var repository = new CourseRepository(context);
        var unitOfWork = new CatalogUnitOfWork(context, new RecordingDispatcher());
        repository.Add(course);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var created = repository.GetVersion(course);

        Assert.True(course.Publish().IsSuccess);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Assert.NotEqual(created, repository.GetVersion(course));
        Assert.Equal(repository.GetVersion(course), await ReadVersionAsync(course.Id));
    }

    [Fact]
    public async Task Adding_a_lesson_does_not_change_the_version_of_the_course()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Lessons Stored Apart");
        await SaveAsync(course);
        var before = await ReadVersionAsync(course.Id);

        await using var context = NewContext();
        var loaded = await new CourseRepository(context).GetAsync(course.Id, cancellationToken);
        Assert.True(loaded!.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        await new CatalogUnitOfWork(context, new RecordingDispatcher()).SaveChangesAsync(cancellationToken);

        Assert.Equal(before, await ReadVersionAsync(course.Id));
    }

    private async Task<uint> ReadVersionAsync(Guid id)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = NewContext();
        var repository = new CourseRepository(context);

        return repository.GetVersion((await repository.GetAsync(id, cancellationToken))!);
    }

    private static Course NewCourse(string title) =>
        Course.Create(Guid.NewGuid(), CourseTitle.Create(title).Value, Money.Create(25m, "USD").Value);

    private async Task SaveAsync(Course course)
    {
        await using var context = NewContext();
        new CourseRepository(context).Add(course);
        await new CatalogUnitOfWork(context, new RecordingDispatcher())
            .SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<bool> ExistsAsync(Guid id)
    {
        await using var context = NewContext();

        return await context.Courses.AnyAsync(course => course.Id == id, TestContext.Current.CancellationToken);
    }

    private CatalogDbContext NewContext() => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_db)
            .Options);

    private sealed class RecordingDispatcher(Func<Task>? onDispatch = null) : IDomainEventDispatcher
    {
        public List<IDomainEvent> Events { get; } = [];

        public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            Events.AddRange(domainEvents);

            if (onDispatch is not null)
            {
                await onDispatch();
            }
        }
    }
}
