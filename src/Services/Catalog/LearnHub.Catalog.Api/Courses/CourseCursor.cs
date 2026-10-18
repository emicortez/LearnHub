using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Api.Courses;

/// <summary>
/// The opaque list cursor: the base64url form of the id to continue after.
/// Clients only pass it back, so its format can change without breaking them.
/// </summary>
public static class CourseCursor
{
    private const int GuidSize = 16;

    // An HTTP concern, not a business rule, so the error lives in the Api rather than in CourseErrors.
    public static readonly Error Invalid = Error.Validation(
        "catalog.course.invalid_cursor", "The cursor is not valid. Use the nextCursor value from a previous page.");

    public static string Encode(Guid id) => Base64Url.EncodeToString(id.ToByteArray(bigEndian: true));

    public static bool TryDecode(string cursor, [NotNullWhen(true)] out Guid? id)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        // Anything that is not exactly 16 base64url-encoded bytes is rejected. IsValid runs first because
        // the decoder throws on illegal characters instead of returning false.
        if (!Base64Url.IsValid(cursor, out var decodedLength) || decodedLength != GuidSize)
        {
            id = null;
            return false;
        }

        Span<byte> bytes = stackalloc byte[GuidSize];
        Base64Url.DecodeFromChars(cursor, bytes);
        id = new Guid(bytes, bigEndian: true);
        return true;
    }
}
