using LearnHub.Catalog.Application.Courses;

using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Catalog use-case handlers.
    /// </summary>
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCourseHandler>();
        services.AddScoped<GetCourseHandler>();

        return services;
    }
}
