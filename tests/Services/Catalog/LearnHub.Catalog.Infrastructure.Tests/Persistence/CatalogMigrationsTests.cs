using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Pins that the migrations and the EF Core model agree. Whether they reproduce each schema rule is proved by
/// <see cref="CatalogSchemaTests"/> and <see cref="CatalogIndexesTests"/>, which run on a migrated database.
/// </summary>
public sealed class CatalogMigrationsTests(CatalogDatabaseFixture database) : IAsyncLifetime
{
    private NpgsqlDataSource _db = null!;

    public async ValueTask InitializeAsync() => _db = await database.CreateEmptyDatabaseAsync();

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Model_has_no_change_without_a_migration()
    {
        await using var context = NewContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Migrated_database_has_every_migration_in_its_history()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = NewContext();

        Assert.NotEmpty(context.Database.GetMigrations());
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(cancellationToken));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(cancellationToken));
    }

    [Fact]
    public async Task Migrating_an_up_to_date_database_changes_nothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = NewContext();
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);

        await context.Database.MigrateAsync(cancellationToken);

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync(cancellationToken));
    }

    private CatalogDbContext NewContext() => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_db)
            .Options);
}
