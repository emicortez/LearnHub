namespace LearnHub.SharedKernel.Tests;

public sealed class ErrorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_code_throws(string code)
    {
        Assert.Throws<ArgumentException>(() => Error.Conflict(code, "The course is full."));
    }

    [Fact]
    public void Create_with_blank_description_throws()
    {
        Assert.Throws<ArgumentException>(() => Error.Conflict("catalog.course.full", " "));
    }

    [Fact]
    public void Validation_factory_creates_a_validation_error()
    {
        var error = Error.Validation("catalog.course.title_required", "The course title is required.");

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("catalog.course.title_required", error.Code);
        Assert.Equal("The course title is required.", error.Description);
    }

    [Fact]
    public void NotFound_factory_creates_a_not_found_error()
    {
        var error = Error.NotFound("catalog.course.not_found", "The course was not found.");

        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("catalog.course.not_found", error.Code);
        Assert.Equal("The course was not found.", error.Description);
    }

    [Fact]
    public void Conflict_factory_creates_a_conflict_error()
    {
        var error = Error.Conflict("catalog.course.full", "The course is full.");

        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal("catalog.course.full", error.Code);
        Assert.Equal("The course is full.", error.Description);
    }

    [Fact]
    public void Errors_with_same_type_code_and_description_are_equal()
    {
        Assert.Equal(
            Error.Conflict("catalog.course.full", "The course is full."),
            Error.Conflict("catalog.course.full", "The course is full."));
    }

    [Fact]
    public void Errors_with_a_different_type_are_not_equal()
    {
        Assert.NotEqual(
            Error.Conflict("catalog.course.full", "The course is full."),
            Error.Validation("catalog.course.full", "The course is full."));
    }
}
