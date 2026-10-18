using System.Collections.Concurrent;

using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Infrastructure.Courses;

/// <summary>
/// A thread-safe, process-local course store, registered as a singleton.
/// Temporary: EF Core replaces it in module 6 without changes to Application or Api.
/// </summary>
public sealed class InMemoryCourseRepository : ICourseRepository
{
    private readonly ConcurrentDictionary<Guid, Course> _courses = new();

    public Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_courses.GetValueOrDefault(id));
    }

    public void Add(Course course)
    {
        ArgumentNullException.ThrowIfNull(course);

        if (!_courses.TryAdd(course.Id, course))
        {
            throw new InvalidOperationException($"A course with id {course.Id} already exists.");
        }
    }
}
