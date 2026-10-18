namespace LearnHub.Catalog.Api.Caching;

public static class OutputCachingServiceCollectionExtensions
{
    public const string CoursesPolicy = "courses";
    public const string CoursesTag = "courses";

    public static IServiceCollection AddCatalogOutputCaching(this IServiceCollection services)
    {
        // The default store is in memory per instance: an eviction only reaches the instance that handled the write,
        // so other instances may serve stale reads until their entries expire. A shared Redis store comes in module 9.
        services.AddOutputCache(cache =>
            cache.AddPolicy(CoursesPolicy, policy => policy
                .Tag(CoursesTag)
                .Expire(TimeSpan.FromMinutes(5))
                // Replaces the default "every query value" rule, so unknown parameters cannot fragment the cache.
                .SetVaryByQuery("limit", "cursor")));

        return services;
    }
}
