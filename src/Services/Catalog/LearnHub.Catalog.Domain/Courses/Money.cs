using System.Globalization;

using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// A monetary amount: non-negative, with at most 2 decimal places, in a 3-letter currency code.
/// </summary>
public sealed class Money : ValueObject
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (amount < 0)
        {
            return CourseErrors.NegativeAmount;
        }

        if (decimal.Round(amount, 2) != amount)
        {
            return CourseErrors.TooManyDecimals;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return CourseErrors.InvalidCurrency;
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(c => c is >= 'A' and <= 'Z'))
        {
            return CourseErrors.InvalidCurrency;
        }

        return new Money(amount, normalized);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount} {Currency}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
