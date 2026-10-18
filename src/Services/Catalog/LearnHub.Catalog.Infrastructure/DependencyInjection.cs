using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Infrastructure.Courses;

using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Catalog infrastructure adapters behind the application ports.
    /// </summary>
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        // Singleton because the in-memory store is the data itself; the EF Core repository will be scoped.
        services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();

        return services;
    }
}
