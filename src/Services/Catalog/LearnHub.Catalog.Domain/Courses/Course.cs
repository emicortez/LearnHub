using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// A course offered by an instructor: starts in draft, gets published once it has lessons,
/// and can be archived afterward.
/// </summary>
public sealed class Course : AggregateRoot
{
    private readonly List<Lesson> _lessons = [];

    private Course(Guid id, Guid instructorId, CourseTitle title, Money price)
        : base(id)
    {
        InstructorId = instructorId;
        Title = title;
        Slug = SlugGenerator.Generate(title.Value);
        Price = price;
        Status = CourseStatus.Draft;
    }

    // For EF Core, which sets the other members from the row. It takes the id because Entity rejects an empty one.
    private Course(Guid id)
        : base(id)
    {
        Title = null!;
        Slug = null!;
        Price = null!;
    }

    public Guid InstructorId { get; }

    public CourseTitle Title { get; }

    public string Slug { get; }

    public Money Price { get; private set; }

    public CourseStatus Status { get; private set; }

    public IReadOnlyList<Lesson> Lessons => _lessons.AsReadOnly();

    public TimeSpan TotalDuration => _lessons.Aggregate(TimeSpan.Zero, (total, lesson) => total + lesson.Duration);

    public static Course Create(Guid instructorId, CourseTitle title, Money price)
    {
        if (instructorId == Guid.Empty)
        {
            throw new ArgumentException("A course needs a non-empty instructor id.", nameof(instructorId));
        }

        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(price);

        return new Course(Guid.CreateVersion7(), instructorId, title, price);
    }

    public Result<Lesson> AddLesson(string? title, TimeSpan duration)
    {
        if (Status != CourseStatus.Draft)
        {
            return Result.Failure<Lesson>(CourseErrors.NotDraft);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Lesson>(CourseErrors.LessonTitleRequired);
        }

        if (duration <= TimeSpan.Zero)
        {
            return Result.Failure<Lesson>(CourseErrors.InvalidDuration);
        }

        var lesson = new Lesson(Guid.CreateVersion7(), title.Trim(), duration);
        _lessons.Add(lesson);

        return lesson;
    }

    public Result ChangePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);

        if (Status == CourseStatus.Archived)
        {
            return CourseErrors.Archived;
        }

        if (newPrice == Price)
        {
            return Result.Success();
        }

        var oldPrice = Price;
        Price = newPrice;

        if (Status == CourseStatus.Published)
        {
            RaiseEvent(new CoursePriceChanged(Id, oldPrice, newPrice));
        }

        return Result.Success();
    }

    public Result Publish()
    {
        if (Status != CourseStatus.Draft)
        {
            return CourseErrors.NotDraft;
        }

        if (_lessons.Count == 0)
        {
            return CourseErrors.NoLessons;
        }

        Status = CourseStatus.Published;
        RaiseEvent(new CoursePublished(Id, Title.Value, Slug, Price));

        return Result.Success();
    }

    public Result Archive()
    {
        if (Status != CourseStatus.Published)
        {
            return CourseErrors.NotPublished;
        }

        Status = CourseStatus.Archived;
        RaiseEvent(new CourseArchived(Id));

        return Result.Success();
    }
}
