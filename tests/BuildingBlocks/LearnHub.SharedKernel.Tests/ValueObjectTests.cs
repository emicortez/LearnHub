namespace LearnHub.SharedKernel.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Value_objects_with_same_values_are_equal()
    {
        Assert.Equal(new TestMoney(50m, "USD"), new TestMoney(50m, "USD"));
    }

    [Theory]
    [InlineData(50, "EUR")]
    [InlineData(80, "USD")]
    public void Value_objects_with_any_different_value_are_not_equal(int amount, string currency)
    {
        Assert.NotEqual(new TestMoney(50m, "USD"), new TestMoney(amount, currency));
    }

    [Fact]
    public void Value_objects_of_different_types_with_same_values_are_not_equal()
    {
        Assert.NotEqual<ValueObject>(new TestMoney(50m, "USD"), new TestPrice(50m, "USD"));
    }

    [Fact]
    public void Equal_value_objects_have_the_same_hash_code()
    {
        var set = new HashSet<TestMoney> { new(50m, "USD") };

        Assert.Contains(new TestMoney(50m, "USD"), set);
    }

    [Fact]
    public void Equality_operators_compare_by_value()
    {
        ValueObject first = new TestMoney(50m, "USD");
        ValueObject second = new TestMoney(50m, "USD");

        Assert.True(first == second);
        Assert.False(first != second);
    }
}

file sealed class TestMoney(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

file sealed class TestPrice(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
