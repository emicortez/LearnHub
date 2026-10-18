using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LearnHub.Catalog.Api.Idempotency;

public static class IdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Singleton because the in-memory store is the data itself; it does not protect across instances
        // and is replaced by a Redis store in module 9.
        services.AddSingleton<IIdempotencyStore>(provider =>
            new InMemoryIdempotencyStore(provider.GetRequiredService<TimeProvider>()));

        return services;
    }
}
