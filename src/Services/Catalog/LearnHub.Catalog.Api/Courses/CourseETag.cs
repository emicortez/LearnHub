using System.Globalization;

using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Api.Courses;

/// <summary>
/// The strong entity tag of a course. It is opaque to clients: they send back what they received.
/// </summary>
public static class CourseETag
{
    public static readonly Error Required = Error.PreconditionRequired(
        "catalog.course.version_required",
        "Send the ETag of the course you read in the If-Match header.");

    public static string Format(uint version) =>
        string.Create(CultureInfo.InvariantCulture, $"\"{version}\"");

    /// <summary>
    /// True when the request names no version. "*" matches any version, which would turn the check off.
    /// </summary>
    public static bool IsMissing(string? ifMatch) =>
        string.IsNullOrWhiteSpace(ifMatch) || ifMatch.Trim() == "*";

    /// <summary>
    /// The version a single strong tag names, or null for anything that can never match: a weak tag, which
    /// <c>If-Match</c> rejects by definition, a list, or a value this API never issues.
    /// </summary>
    public static uint? ParseVersion(string? ifMatch)
    {
        var tag = ifMatch?.Trim();

        // Formatted back, because tags compare as opaque strings: "07" is not the tag "7".
        return tag is ['"', .. var version, '"']
            && uint.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            && Format(parsed) == tag
                ? parsed
                : null;
    }
}
