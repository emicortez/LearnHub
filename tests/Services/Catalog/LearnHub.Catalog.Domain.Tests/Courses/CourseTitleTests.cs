using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Domain.Tests.Courses;

public sealed class CourseTitleTests
{
    [Fact]
    public void Create_trims_the_value()
    {
        var result = CourseTitle.Create("  Clean Code  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Clean Code", result.Value.Value);
    }

    [Fact]
    public void ToString_returns_the_value()
    {
        var title = CourseTitle.Create("Clean Code").Value;

        Assert.Equal("Clean Code", title.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_blank_value_fails(string? value)
    {
        var result = CourseTitle.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.TitleRequired, result.Error);
    }

    [Fact]
    public void Create_value_longer_than_120_characters_fails()
    {
        var result = CourseTitle.Create(new string('a', 121));

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.TitleTooLong, result.Error);
    }

    [Fact]
    public void Create_value_with_exactly_120_characters_after_trim_succeeds()
    {
        var result = CourseTitle.Create($"  {new string('a', 120)}  ");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Two_titles_with_the_same_value_are_equal()
    {
        var first = CourseTitle.Create("Clean Code").Value;
        var second = CourseTitle.Create("Clean Code").Value;

        Assert.Equal(first, second);
    }
}
