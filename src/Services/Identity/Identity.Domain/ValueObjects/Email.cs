using Identity.Domain.Errors;
using Identity.Domain.Primitives;

namespace Identity.Domain.ValueObjects;

// El Email sabe qué es un email válido. Nadie más necesita saberlo.
// Si el formato es inválido, nunca vas a tener un Email inválido en el sistema.
// Eso se llama "making illegal states unrepresentable".
public sealed class Email : ValueObject
{
    private const int MaxLength = 320;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(UserErrors.Email.Empty, "Email is required.");

        value = value.Trim().ToLowerInvariant();

        if (value.Length > MaxLength)
            throw new DomainException(UserErrors.Email.TooLong, $"Email must be {MaxLength} characters or fewer.");

        if (!value.Contains('@') || !value.Contains('.'))
            throw new DomainException(UserErrors.Email.InvalidFormat, "Email format is invalid.");

        return new Email(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
