namespace LearnHub.Catalog.Api.Authentication;

/// <summary>
/// The authorization policies of Catalog. Roles are Entra app roles assigned to users; the scope is what the client
/// application was allowed to do on the user's behalf. Both must hold.
/// </summary>
public static class CatalogPolicies
{
    /// <summary>An approved instructor, acting through an app granted <see cref="WriteScope"/>.</summary>
    public const string Instructor = "Instructor";

    public const string InstructorRole = "instructor";

    public const string WriteScope = "courses.write";
}
