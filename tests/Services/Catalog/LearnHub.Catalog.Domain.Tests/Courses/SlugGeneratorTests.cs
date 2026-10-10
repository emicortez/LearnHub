using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Domain.Tests.Courses;

public sealed class SlugGeneratorTests
{
    [Fact]
    public void Generate_single_word_returns_it_in_lowercase()
    {
        var slug = SlugGenerator.Generate("Docker");

        Assert.Equal("docker", slug);
    }

    [Fact]
    public void Generate_multiple_words_joins_them_with_hyphens()
    {
        var slug = SlugGenerator.Generate("Clean Code");

        Assert.Equal("clean-code", slug);
    }

    [Fact]
    public void Generate_trims_and_collapses_extra_whitespace()
    {
        var slug = SlugGenerator.Generate("  Clean   Code  ");

        Assert.Equal("clean-code", slug);
    }

    [Fact]
    public void Generate_removes_accents()
    {
        var slug = SlugGenerator.Generate("Introducción a Programación");

        Assert.Equal("introduccion-a-programacion", slug);
    }

    [Fact]
    public void Generate_removes_symbols()
    {
        var slug = SlugGenerator.Generate("C# & .NET 10");

        Assert.Equal("c-net-10", slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_blank_title_throws(string title)
    {
        Assert.Throws<ArgumentException>(() => SlugGenerator.Generate(title));
    }

    [Fact]
    public void Generate_title_without_letters_or_digits_throws()
    {
        Assert.Throws<ArgumentException>(() => SlugGenerator.Generate("#&!"));
    }
}
