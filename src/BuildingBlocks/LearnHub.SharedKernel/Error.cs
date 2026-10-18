using System.Diagnostics.CodeAnalysis;

namespace LearnHub.SharedKernel;

/// <summary>
/// An expected business failure, identified by a stable code in the form
/// <c>{service}.{entity}.{problem}</c> (for example <c>catalog.course.full</c>) and classified by an <see cref="ErrorType"/>.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "'Error' is only a keyword in Visual Basic; this C#-only shared kernel keeps the conventional name.")]
public sealed record Error
{
    // Private so every error is created through a factory and always carries an explicit type.
    private Error(string code, string description, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}
