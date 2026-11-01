using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;

using Microsoft.EntityFrameworkCore;

namespace LearnHub.Catalog.Infrastructure.Persistence;

/// <summary>
/// Loads and adds <see cref="Course"/> aggregates through the request's <see cref="CatalogDbContext"/>.
/// Whole aggregates only, with their lessons in order, so a handler can never work on a partial course.
/// It never saves: <see cref="CatalogUnitOfWork"/> commits what the handler changed.
/// </summary>
public sealed class CourseRepository(CatalogDbContext context) : ICourseRepository
{
    public Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.CoursesWithOrderedLessons.SingleOrDefaultAsync(course => course.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Course>> ListAsync(Guid? afterId, int take, CancellationToken cancellationToken) =>
        await context.CoursesWithOrderedLessons
            .Where(course => afterId == null || course.Id > afterId)
            .OrderBy(course => course.Id)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Course course) => context.Courses.Add(course);

    public uint GetVersion(Course course) =>
        context.Entry(course).Property<uint>(CourseConfiguration.Version).CurrentValue;
}
