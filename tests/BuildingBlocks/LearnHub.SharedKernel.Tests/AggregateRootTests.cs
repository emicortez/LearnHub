namespace LearnHub.SharedKernel.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void New_aggregate_has_no_domain_events()
    {
        var course = new TestCourse(Guid.NewGuid());

        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void Raising_an_event_records_it()
    {
        var course = new TestCourse(Guid.NewGuid());
        var published = new TestCoursePublished(course.Id);

        course.Publish(published);

        Assert.Equal([published], course.DomainEvents);
    }

    [Fact]
    public void Clearing_events_removes_all_recorded_events()
    {
        var course = new TestCourse(Guid.NewGuid());
        course.Publish(new TestCoursePublished(course.Id));

        course.ClearDomainEvents();

        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void Domain_events_cannot_be_modified_from_outside()
    {
        var course = new TestCourse(Guid.NewGuid());

        Assert.Throws<InvalidCastException>(() => (List<IDomainEvent>)course.DomainEvents);
    }
}

file sealed record TestCoursePublished(Guid CourseId) : IDomainEvent;

file sealed class TestCourse(Guid id) : AggregateRoot(id)
{
    public void Publish(TestCoursePublished domainEvent) => RaiseEvent(domainEvent);
}
