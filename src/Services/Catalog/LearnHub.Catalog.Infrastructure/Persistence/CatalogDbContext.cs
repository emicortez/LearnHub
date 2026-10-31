using LearnHub.Catalog.Domain.Courses;

using Microsoft.EntityFrameworkCore;

namespace LearnHub.Catalog.Infrastructure.Persistence;

/// <summary>
/// One unit of work against the Catalog database: short-lived and not thread-safe, so one instance per request.
/// The schema is the contract in Persistence/Schema/*.sql; this model maps onto it.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
}
