using Npgsql;

using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(LearnHub.Catalog.Infrastructure.Tests.Persistence.CatalogDatabaseFixture))]

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// One real PostgreSQL container for the whole test run, with every Catalog schema script applied in order.
/// Uses the same image the Aspire AppHost runs, so the tests prove the engine the app talks to.
/// </summary>
public sealed class CatalogDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.3")
        .WithDatabase("catalog")
        .Build();

    private NpgsqlDataSource? _dataSource;

    public NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException("The database is not started yet.");

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        _dataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
        await ApplySchemaAsync(_dataSource);
    }

    /// <summary>
    /// Creates a new database with the schema applied and no rows, for tests that read whole tables
    /// and must not see rows other tests inserted into the shared database. The caller disposes it.
    /// </summary>
    public async Task<NpgsqlDataSource> CreateEmptyDatabaseAsync()
    {
        var name = $"catalog_{Guid.CreateVersion7():N}";
        await using (var create = DataSource.CreateCommand($"CREATE DATABASE {name}"))
        {
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name };
        var dataSource = NpgsqlDataSource.Create(connectionString.ConnectionString);
        await ApplySchemaAsync(dataSource);
        return dataSource;
    }

    private static async Task ApplySchemaAsync(NpgsqlDataSource dataSource)
    {
        var scripts = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Schema"), "*.sql")
            .Order(StringComparer.Ordinal);

        foreach (var script in scripts)
        {
            await using var command = dataSource.CreateCommand(await File.ReadAllTextAsync(script));
            await command.ExecuteNonQueryAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
