using LearnHub.Catalog.Application;
using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Catalog infrastructure adapters behind the application ports.
    /// </summary>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("catalog")
            ?? throw new InvalidOperationException("The connection string 'catalog' is not configured.");

        // Scoped by default: one context per request, shared by the repository and the unit of work below, so what
        // the repository tracks is what the unit of work commits. A context is not thread-safe, so never a singleton.
        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseQueries, CourseQueries>();
        services.AddScoped<IUnitOfWork, CatalogUnitOfWork>();
        services.AddSingleton<IDomainEventDispatcher, LoggingDomainEventDispatcher>();

        return services;
    }
}
