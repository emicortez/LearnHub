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

    public Task<IReadOnlyList<Course>> ListAsync(Guid? afterId, int take, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Ids are unique and Guid comparison is a total order, so the sort is stable across calls,
        // which is all keyset pagination needs (v7 ids from the same millisecond are not creation-ordered).
        // A snapshot of the dictionary is enumerated, so concurrent adds cannot break the query.
        IReadOnlyList<Course> page = _courses.Values
            .Where(course => afterId is null || course.Id.CompareTo(afterId.Value) > 0)
            .OrderBy(course => course.Id)
            .Take(take)
            .ToList();

        return Task.FromResult(page);
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
