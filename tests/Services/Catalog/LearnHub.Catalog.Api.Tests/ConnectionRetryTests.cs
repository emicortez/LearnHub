using System.Data.Common;

using LearnHub.Catalog.Application;
using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure;
using LearnHub.Catalog.Infrastructure.Persistence;
using LearnHub.Catalog.Infrastructure.Tests.Persistence;
using LearnHub.SharedKernel;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// Pins what the retries of the real registration do, on a database of its own: they recover from a lost connection,
/// they never repeat a failure that a new attempt cannot fix, and what a commit with an unknown outcome really gives.
/// </summary>
public sealed class ConnectionRetryTests : IAsyncDisposable
{
    private readonly CommandRecorder _recorder = new();
    private readonly WriteThatLosesItsAnswer _lostCommit = new();

    private readonly string _connectionString;
    private readonly ServiceProvider _services;

    public ConnectionRetryTests()
    {
        // Pooled, unlike the API tests: a closed connection is only a failure if it is reused.
        _connectionString = new NpgsqlConnectionStringBuilder(CatalogApiDatabase.CreateDatabase())
        {
            Pooling = true,
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:catalog"] = _connectionString })
            .Build();

        _services = new ServiceCollection()
            .AddLogging()
            .AddCatalogInfrastructure(configuration)
            .ConfigureDbContext<CatalogDbContext>(options => options.AddInterceptors(_recorder, _lostCommit))
            .BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task A_read_recovers_from_a_connection_the_server_closed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Survivor");
        await AddAsync(course);
        Assert.Equal("Survivor", (await GetAsync(course.Id))!.Title);

        Assert.True(await CloseServerConnectionsAsync(cancellationToken) > 0);

        Assert.Equal("Survivor", (await GetAsync(course.Id))!.Title);
    }

    [Fact]
    public async Task A_taken_slug_is_not_retried()
    {
        await AddAsync(NewCourse("Same Title"));
        _recorder.Clear();

        var saved = await AddAsync(NewCourse("Same Title"));

        Assert.Equal(CourseErrors.SlugTaken, saved.Error);
        Assert.Single(_recorder.Commands, command => command.Text.Contains("INSERT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_stale_version_is_not_retried()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var course = NewCourse("Stale");
        Assert.True(course.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        await AddAsync(course);

        await using var stale = _services.CreateAsyncScope();
        var staleCourse = await stale.ServiceProvider.GetRequiredService<ICourseRepository>()
            .GetAsync(course.Id, cancellationToken);
        await PublishAsync(course.Id);
        _recorder.Clear();

        Assert.True(staleCourse!.Publish().IsSuccess);
        var saved = await stale.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        Assert.Equal(CourseErrors.VersionMismatch, saved.Error);
        Assert.Single(_recorder.Commands, command => command.Text.Contains("UPDATE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_update_whose_commit_lost_its_answer_is_stored_but_reported_as_a_version_mismatch()
    {
        var course = NewCourse("Doubtful Update");
        Assert.True(course.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        await AddAsync(course);

        _lostCommit.LoseNext();
        var saved = await PublishAsync(course.Id);

        // The retry sends the old xmin, which the first commit already changed: it matches no row.
        Assert.Equal(CourseErrors.VersionMismatch, saved.Error);
        Assert.Equal("Published", (await GetAsync(course.Id))!.Status);
    }

    [Fact]
    public async Task An_insert_whose_commit_lost_its_answer_is_stored_once_and_fails_on_its_own_key()
    {
        var course = NewCourse("Doubtful Insert");

        _lostCommit.LoseNext();
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(course));

        // The retry inserts the same id, so the course exists while the caller sees a failure.
        var violation = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, violation.SqlState);
        Assert.Equal("pk_courses", violation.ConstraintName);
        Assert.Equal("Draft", (await GetAsync(course.Id))!.Status);
    }

    private static Course NewCourse(string title) =>
        Course.Create(Guid.NewGuid(), CourseTitle.Create(title).Value, Money.Create(25m, "USD").Value);

    private async Task<CourseDetails?> GetAsync(Guid id)
    {
        await using var scope = _services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<ICourseQueries>()
            .GetAsync(id, TestContext.Current.CancellationToken);
    }

    private async Task<Result> AddAsync(Course course)
    {
        await using var scope = _services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICourseRepository>().Add(course);

        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>()
            .SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Result> PublishAsync(Guid id)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = _services.CreateAsyncScope();
        var course = await scope.ServiceProvider.GetRequiredService<ICourseRepository>().GetAsync(id, cancellationToken);
        Assert.True(course!.Publish().IsSuccess);

        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private async Task<long> CloseServerConnectionsAsync(CancellationToken cancellationToken)
    {
        var separate = new NpgsqlConnectionStringBuilder(_connectionString) { Pooling = false };
        await using var connection = new NpgsqlConnection(separate.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT count(pg_terminate_backend(pid))
            FROM pg_stat_activity
            WHERE datname = current_database() AND pid <> pg_backend_pid()
            """,
            connection);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    // Fails right after the write, as a connection cut before the answer arrives would. A save of one statement has no
    // explicit transaction: the statement is its own commit, so at this point it is already stored.
    private sealed class WriteThatLosesItsAnswer : DbCommandInterceptor
    {
        private bool _loseNext;

        public void LoseNext() => _loseNext = true;

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (_loseNext && !command.CommandText.StartsWith("SELECT", StringComparison.Ordinal))
            {
                _loseNext = false;
                await result.DisposeAsync();
                throw new NpgsqlException("The connection was lost during the commit.", new TimeoutException());
            }

            return result;
        }
    }
}
