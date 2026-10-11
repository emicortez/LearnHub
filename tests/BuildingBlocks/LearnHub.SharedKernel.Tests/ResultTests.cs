namespace LearnHub.SharedKernel.Tests;

public sealed class ResultTests
{
    private static readonly Error CourseFull = new("catalog.course.full", "The course is full.");

    [Fact]
    public void Success_exposes_the_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_exposes_the_error()
    {
        var result = Result.Failure<int>(CourseFull);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseFull, result.Error);
    }

    [Fact]
    public void Reading_the_value_of_a_failure_throws()
    {
        var result = Result.Failure<int>(CourseFull);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Reading_the_error_of_a_success_throws()
    {
        var result = Result.Success(42);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void A_value_converts_implicitly_to_a_success()
    {
        Result<int> result = 42;

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void An_error_converts_implicitly_to_a_failure()
    {
        Result<int> result = CourseFull;

        Assert.Equal(CourseFull, result.Error);
    }

    [Fact]
    public void Result_without_value_can_succeed_or_fail()
    {
        Assert.True(Result.Success().IsSuccess);
        Assert.Equal(CourseFull, Result.Failure(CourseFull).Error);
    }

    [Fact]
    public void Failure_without_error_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }
}
