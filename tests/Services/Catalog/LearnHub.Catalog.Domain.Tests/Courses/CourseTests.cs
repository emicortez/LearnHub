using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Domain.Tests.Courses;

public sealed class CourseTests
{
    private static readonly CourseTitle Title = CourseTitle.Create("Clean Code").Value;
    private static readonly Money Price = Money.Create(19.99m, "USD").Value;

    [Fact]
    public void Create_sets_the_initial_state()
    {
        var instructorId = Guid.CreateVersion7();

        var course = Course.Create(instructorId, Title, Price);

        Assert.NotEqual(Guid.Empty, course.Id);
        Assert.Equal(instructorId, course.InstructorId);
        Assert.Equal(Title, course.Title);
        Assert.Equal("clean-code", course.Slug);
        Assert.Equal(Price, course.Price);
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.Empty(course.Lessons);
        Assert.Equal(TimeSpan.Zero, course.TotalDuration);
        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void Create_with_empty_instructor_id_throws()
    {
        Assert.Throws<ArgumentException>(() => Course.Create(Guid.Empty, Title, Price));
    }

    [Fact]
    public void Create_with_null_title_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Course.Create(Guid.CreateVersion7(), null!, Price));
    }

    [Fact]
    public void Create_with_null_price_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Course.Create(Guid.CreateVersion7(), Title, null!));
    }

    [Fact]
    public void AddLesson_in_draft_succeeds_and_appends_the_lesson()
    {
        var course = CreateDraftCourse();

        var result = course.AddLesson("Introduction", TimeSpan.FromMinutes(10));

        Assert.True(result.IsSuccess);
        Assert.Single(course.Lessons);
        Assert.Equal("Introduction", course.Lessons[0].Title);
        Assert.Equal(TimeSpan.FromMinutes(10), course.Lessons[0].Duration);
        Assert.Equal(TimeSpan.FromMinutes(10), course.TotalDuration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLesson_with_blank_title_fails(string? title)
    {
        var course = CreateDraftCourse();

        var result = course.AddLesson(title, TimeSpan.FromMinutes(10));

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.LessonTitleRequired, result.Error);
    }

    [Fact]
    public void AddLesson_with_zero_duration_fails()
    {
        var course = CreateDraftCourse();

        var result = course.AddLesson("Introduction", TimeSpan.Zero);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.InvalidDuration, result.Error);
    }

    [Fact]
    public void AddLesson_with_a_fraction_of_a_second_fails()
    {
        var course = CreateDraftCourse();

        var result = course.AddLesson("Introduction", TimeSpan.FromSeconds(1.5));

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.DurationNotWholeSeconds, result.Error);
        Assert.Empty(course.Lessons);
    }

    [Fact]
    public void AddLesson_when_not_draft_fails()
    {
        var course = CreatePublishedCourse();

        var result = course.AddLesson("Introduction", TimeSpan.FromMinutes(10));

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.NotDraft, result.Error);
    }

    [Fact]
    public void ChangePrice_in_draft_updates_the_price_without_raising_an_event()
    {
        var course = CreateDraftCourse();
        var newPrice = Money.Create(29.99m, "USD").Value;

        var result = course.ChangePrice(newPrice);

        Assert.True(result.IsSuccess);
        Assert.Equal(newPrice, course.Price);
        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void ChangePrice_to_the_same_price_succeeds_without_raising_an_event()
    {
        var course = CreatePublishedCourse();
        course.ClearDomainEvents();

        var result = course.ChangePrice(Price);

        Assert.True(result.IsSuccess);
        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void ChangePrice_when_published_raises_CoursePriceChanged()
    {
        var course = CreatePublishedCourse();
        course.ClearDomainEvents();
        var newPrice = Money.Create(29.99m, "USD").Value;

        var result = course.ChangePrice(newPrice);

        Assert.True(result.IsSuccess);
        Assert.Equal(newPrice, course.Price);
        var raised = Assert.Single(course.DomainEvents);
        var priceChanged = Assert.IsType<CoursePriceChanged>(raised);
        Assert.Equal(course.Id, priceChanged.CourseId);
        Assert.Equal(Price, priceChanged.OldPrice);
        Assert.Equal(newPrice, priceChanged.NewPrice);
    }

    [Fact]
    public void ChangePrice_when_archived_fails()
    {
        var course = CreateArchivedCourse();
        var newPrice = Money.Create(29.99m, "USD").Value;

        var result = course.ChangePrice(newPrice);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.Archived, result.Error);
    }

    [Fact]
    public void ChangePrice_with_null_price_throws()
    {
        var course = CreateDraftCourse();

        Assert.Throws<ArgumentNullException>(() => course.ChangePrice(null!));
    }

    [Fact]
    public void Publish_without_lessons_fails()
    {
        var course = CreateDraftCourse();

        var result = course.Publish();

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.NoLessons, result.Error);
    }

    [Fact]
    public void Publish_with_lessons_succeeds_and_raises_CoursePublished()
    {
        var course = CreateDraftCourse();
        course.AddLesson("Introduction", TimeSpan.FromMinutes(10));

        var result = course.Publish();

        Assert.True(result.IsSuccess);
        Assert.Equal(CourseStatus.Published, course.Status);
        var raised = Assert.Single(course.DomainEvents);
        var published = Assert.IsType<CoursePublished>(raised);
        Assert.Equal(course.Id, published.CourseId);
        Assert.Equal(Title.Value, published.Title);
        Assert.Equal(course.Slug, published.Slug);
        Assert.Equal(Price, published.Price);
    }

    [Fact]
    public void Publish_when_already_published_fails()
    {
        var course = CreatePublishedCourse();

        var result = course.Publish();

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.NotDraft, result.Error);
    }

    [Fact]
    public void Archive_when_published_succeeds_and_raises_CourseArchived()
    {
        var course = CreatePublishedCourse();
        course.ClearDomainEvents();

        var result = course.Archive();

        Assert.True(result.IsSuccess);
        Assert.Equal(CourseStatus.Archived, course.Status);
        var raised = Assert.Single(course.DomainEvents);
        var archived = Assert.IsType<CourseArchived>(raised);
        Assert.Equal(course.Id, archived.CourseId);
    }

    [Fact]
    public void Archive_when_not_published_fails()
    {
        var course = CreateDraftCourse();

        var result = course.Archive();

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.NotPublished, result.Error);
    }

    private static Course CreateDraftCourse() => Course.Create(Guid.CreateVersion7(), Title, Price);

    private static Course CreatePublishedCourse()
    {
        var course = CreateDraftCourse();
        course.AddLesson("Introduction", TimeSpan.FromMinutes(10));
        course.Publish();
        return course;
    }

    private static Course CreateArchivedCourse()
    {
        var course = CreatePublishedCourse();
        course.Archive();
        return course;
    }
}
