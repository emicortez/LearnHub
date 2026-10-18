namespace LearnHub.Catalog.Api.Viewers;

public static class CourseViewersServiceCollectionExtensions
{
    public const string HubPath = "/hubs/course-viewers";

    public static IServiceCollection AddCourseViewers(this IServiceCollection services)
    {
        services.AddSignalR();

        // Singleton because the in-memory store is the data itself. Hubs are created per invocation,
        // so presence can never live in a hub field.
        services.AddSingleton<ICourseViewerPresence, InMemoryCourseViewerPresence>();

        return services;
    }

    public static IEndpointRouteBuilder MapCourseViewers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<CourseViewersHub>(HubPath);

        return endpoints;
    }
}
