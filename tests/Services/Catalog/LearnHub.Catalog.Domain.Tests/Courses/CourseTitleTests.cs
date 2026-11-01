using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Domain.Tests.Courses;

public sealed class CourseTitleTests
{
    [Theory]
    [InlineData("!!!")]
    [InlineData("---")]
    [InlineData("\u0301")] // A lone combining accent.
    [InlineData("\U0001F600")] // An emoji.
    public void Create_without_a_letter_or_digit_fails(string value)
    {
        Assert.Equal(CourseErrors.TitleWithoutLetters, CourseTitle.Create(value).Error);
    }

    [Theory]
    [InlineData("C#")]
    [InlineData("100")]
    [InlineData("Introducción")]
    public void Create_with_one_letter_or_digit_succeeds_and_gives_a_slug(string value)
    {
        var title = CourseTitle.Create(value);

        Assert.True(title.IsSuccess);
        Assert.NotEmpty(SlugGenerator.Generate(title.Value.Value));
    }

    [Theory]
    [InlineData("Null\0Character")]
    [InlineData("Probe \uFFFE title")] // A noncharacter, which string.Normalize rejects.
    public void Create_with_an_invalid_character_fails(string value)
    {
        Assert.Equal(CourseErrors.TitleInvalidCharacter, CourseTitle.Create(value).Error);
    }

    [Fact]
    public void Create_with_a_lone_surrogate_fails()
    {
        // Not an InlineData: the test runner replaces a lone surrogate when it serializes the arguments.
        Assert.Equal(CourseErrors.TitleInvalidCharacter, CourseTitle.Create("Probe \uD800 title").Error);
    }

    [Fact]
    public void Create_keeps_a_title_of_one_compatibility_ideograph()
    {
        // A surrogate pair, so no single char is a letter, but it normalizes to one.
        Assert.True(CourseTitle.Create("\U0002F800").IsSuccess);
    }

    [Fact]
    public void Every_title_accepted_gives_a_slug()
    {
        for (var codePoint = 1; codePoint <= 0x10FFFF; codePoint++)
        {
            if (codePoint is >= 0xD800 and <= 0xDFFF)
            {
                continue;
            }

            var title = CourseTitle.Create(char.ConvertFromUtf32(codePoint));

            if (title.IsSuccess)
            {
                Assert.NotEmpty(SlugGenerator.Generate(title.Value.Value));
            }
        }
    }

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
