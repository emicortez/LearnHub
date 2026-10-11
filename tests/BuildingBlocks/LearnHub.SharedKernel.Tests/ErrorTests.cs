namespace LearnHub.SharedKernel.Tests;

public sealed class ErrorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_code_throws(string code)
    {
        Assert.Throws<ArgumentException>(() => new Error(code, "The course is full."));
    }

    [Fact]
    public void Create_with_blank_description_throws()
    {
        Assert.Throws<ArgumentException>(() => new Error("catalog.course.full", " "));
    }

    [Fact]
    public void Errors_with_same_code_and_description_are_equal()
    {
        Assert.Equal(
            new Error("catalog.course.full", "The course is full."),
            new Error("catalog.course.full", "The course is full."));
    }
}
