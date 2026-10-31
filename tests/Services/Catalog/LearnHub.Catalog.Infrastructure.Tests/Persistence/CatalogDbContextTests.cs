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

        var update = Assert.Single(_commands);
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
        await using var context = NewContext();
        context.Courses.Add(course);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return course;
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
