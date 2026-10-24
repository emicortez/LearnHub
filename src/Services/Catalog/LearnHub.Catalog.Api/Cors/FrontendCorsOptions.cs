namespace LearnHub.Catalog.Api.Cors;

/// <summary>
/// The browser origins whose scripts may read Catalog's responses, bound from <c>Cors</c>.
/// Empty by default: with the BFF (ADR 0010) the browser never calls Catalog directly, so only an explicit exception,
/// such as a local development frontend, is ever listed.
/// </summary>
public sealed class FrontendCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
