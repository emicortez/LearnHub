using System.Diagnostics.CodeAnalysis;

namespace LearnHub.SharedKernel;

/// <summary>
/// An expected business failure, identified by a stable code in the form
/// <c>{service}.{entity}.{problem}</c> (for example <c>catalog.course.full</c>).
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "'Error' is only a keyword in Visual Basic; this C#-only shared kernel keeps the conventional name.")]
public sealed record Error
{
    public Error(string code, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Code = code;
        Description = description;
    }

    public string Code { get; }

    public string Description { get; }
}
