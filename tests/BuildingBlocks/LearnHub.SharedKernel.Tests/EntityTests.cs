namespace LearnHub.SharedKernel.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Create_with_empty_id_throws()
    {
        Assert.Throws<ArgumentException>(() => new TestCourse(Guid.Empty));
    }

    [Fact]
    public void Entities_of_same_type_with_same_id_are_equal()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestCourse(id), new TestCourse(id));
    }

    [Fact]
    public void Entities_of_different_types_with_same_id_are_not_equal()
    {
        var id = Guid.NewGuid();

        Assert.NotEqual<Entity>(new TestCourse(id), new TestLesson(id));
    }

    [Fact]
    public void Equality_operators_compare_by_id()
    {
        var id = Guid.NewGuid();
        Entity first = new TestCourse(id);
        Entity second = new TestCourse(id);

        Assert.True(first == second);
        Assert.False(first != second);
    }
}

file sealed class TestCourse(Guid id) : Entity(id);

file sealed class TestLesson(Guid id) : Entity(id);
