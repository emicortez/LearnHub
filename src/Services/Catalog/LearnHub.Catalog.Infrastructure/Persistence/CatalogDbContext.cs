using LearnHub.Catalog.Domain.Courses;

using Microsoft.EntityFrameworkCore;

namespace LearnHub.Catalog.Infrastructure.Persistence;

/// <summary>
/// One unit of work against the Catalog database: short-lived and not thread-safe, so one instance per request.
/// The model and the reviewed migrations in Persistence/Migrations define the schema.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    /// <summary>
    /// Courses with their lessons in the aggregate's order. AutoInclude cannot sort, so any read that depends on
    /// the order of <see cref="Course.Lessons"/> goes through here.
    /// </summary>
    public IQueryable<Course> CoursesWithOrderedLessons =>
        Courses.Include(course => course.Lessons
            .OrderBy(lesson => EF.Property<int>(lesson, LessonConfiguration.Position)));

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignPositionsToNewLessons();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AssignPositionsToNewLessons();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
        modelBuilder.ApplyConfiguration(new LessonConfiguration());
    }

    // Lessons are only appended, so a new one goes after the highest stored position. Stored lessons keep theirs:
    // they may be loaded in any order, so the index in the list says nothing about them.
    private void AssignPositionsToNewLessons()
    {
        foreach (var course in ChangeTracker.Entries<Course>().Select(entry => entry.Entity).ToList())
        {
            var lessons = course.Lessons
                .Select(lesson => Entry(lesson))
                .ToList();

            var position = lessons
                .Where(lesson => lesson.State != EntityState.Added)
                .Select(lesson => lesson.Property<int>(LessonConfiguration.Position).CurrentValue)
                .DefaultIfEmpty()
                .Max();

            foreach (var lesson in lessons.Where(lesson => lesson.State == EntityState.Added))
            {
                lesson.Property<int>(LessonConfiguration.Position).CurrentValue = ++position;
            }
        }
    }
}
