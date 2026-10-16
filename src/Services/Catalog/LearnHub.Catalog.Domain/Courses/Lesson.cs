using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// A single lesson within a <see cref="Course"/>. Only <see cref="Course.AddLesson"/> creates lessons.
/// </summary>
public sealed class Lesson : Entity
{
    internal Lesson(Guid id, string title, TimeSpan duration)
        : base(id)
    {
        Title = title;
        Duration = duration;
    }

    public string Title { get; }

    public TimeSpan Duration { get; }
}
