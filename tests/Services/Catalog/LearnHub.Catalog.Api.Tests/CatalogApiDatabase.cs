using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(LearnHub.Catalog.Api.Tests.CatalogApiDatabase))]

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// One real PostgreSQL container for the whole test run, with a migrated template database. Every API host gets its
/// own copy of the template, so tests that share a host see each other's rows and tests on another host do not.
/// Uses the same image the Aspire AppHost runs.
/// </summary>
public sealed class CatalogApiDatabase : IAsyncLifetime
{
    private const string Template = "catalog_template";

    // Static because hosts are created by parameterless factories, some by xUnit and some inside a test.
    private static CatalogApiDatabase? _current;

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.3")
        .WithDatabase(Template)
        .Build();

    /// <summary>
    /// Creates an empty migrated database and returns its connection string.
    /// </summary>
    public static string CreateDatabase()
    {
        var database = _current ?? throw new InvalidOperationException("The database is not started yet.");
        var name = $"catalog_{Guid.CreateVersion7():N}";

        using var connection = new NpgsqlConnection(database.ConnectionString("postgres"));
        connection.Open();
        using var create = new NpgsqlCommand($"CREATE DATABASE {name} TEMPLATE {Template}", connection);
        create.ExecuteNonQuery();

        return database.ConnectionString(name);
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(ConnectionString(Template))
                .Options))
        {
            await context.Database.MigrateAsync();
        }

        _current = this;
    }

    public async ValueTask DisposeAsync()
    {
        _current = null;
        await _container.DisposeAsync();
    }

    // Without pooling: PostgreSQL refuses to copy a template with open connections, and each host's pool would
    // keep connections open past the host, until the run hit max_connections.
    private string ConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database,
            Pooling = false,
        }.ConnectionString;
}
