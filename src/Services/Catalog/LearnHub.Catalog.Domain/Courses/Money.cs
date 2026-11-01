using System.Globalization;

using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// A monetary amount: from 0 to <see cref="MaxAmount"/>, with at most 2 decimal places, in a 3-letter currency code.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>The highest amount stored; ck_courses_price_range enforces the same limit.</summary>
    public const decimal MaxAmount = 9999999999.99m;

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

        if (amount > MaxAmount)
        {
            return CourseErrors.AmountTooLarge;
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
