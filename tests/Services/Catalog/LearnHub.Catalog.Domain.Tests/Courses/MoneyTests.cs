using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Catalog.Domain.Tests.Courses;

public sealed class MoneyTests
{
    [Fact]
    public void Create_valid_amount_and_currency_succeeds()
    {
        var result = Money.Create(19.99m, "usd");

        Assert.True(result.IsSuccess);
        Assert.Equal(19.99m, result.Value.Amount);
        Assert.Equal("USD", result.Value.Currency);
    }

    [Fact]
    public void Create_trims_and_uppercases_the_currency()
    {
        var result = Money.Create(10m, "  eur  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.Currency);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Create_negative_amount_fails(decimal amount)
    {
        var result = Money.Create(amount, "USD");

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.NegativeAmount, result.Error);
    }

    [Fact]
    public void Create_zero_amount_succeeds()
    {
        var result = Money.Create(0m, "USD");

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(1.001)]
    [InlineData(1.999)]
    public void Create_amount_with_more_than_2_decimal_places_fails(decimal amount)
    {
        var result = Money.Create(amount, "USD");

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.TooManyDecimals, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    public void Create_invalid_currency_fails(string? currency)
    {
        var result = Money.Create(10m, currency);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.InvalidCurrency, result.Error);
    }

    [Fact]
    public void Two_amounts_with_the_same_amount_and_currency_are_equal()
    {
        var first = Money.Create(10m, "USD").Value;
        var second = Money.Create(10m, "USD").Value;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Two_amounts_with_different_currencies_are_not_equal()
    {
        var first = Money.Create(10m, "USD").Value;
        var second = Money.Create(10m, "EUR").Value;

        Assert.NotEqual(first, second);
    }
}
