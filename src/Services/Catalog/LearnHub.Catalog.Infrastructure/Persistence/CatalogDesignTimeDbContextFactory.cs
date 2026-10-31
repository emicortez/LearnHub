using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnHub.Catalog.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` build the context without starting the Api. Generating a migration compares the model with its
/// snapshot, so it needs the provider and never connects to a database.
/// </summary>
public sealed class CatalogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql()
            .Options);
}
