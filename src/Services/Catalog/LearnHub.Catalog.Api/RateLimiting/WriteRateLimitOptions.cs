namespace LearnHub.Catalog.Api.RateLimiting;

/// <summary>
/// The token bucket for the <c>writes</c> policy, bound from <c>RateLimiting:Writes</c>.
/// Each client starts with <see cref="TokenLimit"/> writes and gets <see cref="TokensPerPeriod"/> back every period.
/// </summary>
public sealed class WriteRateLimitOptions
{
    public const string SectionName = "RateLimiting:Writes";

    public int TokenLimit { get; set; } = 10;

    public int TokensPerPeriod { get; set; } = 10;

    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromMinutes(1);
}
