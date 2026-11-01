using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// Runs the check a host runs before it starts. A real host is no use here: when startup fails,
/// <c>WebApplicationFactory</c> races to read a provider that is already disposed, and may throw that instead.
/// </summary>
internal static class StartupValidation
{
    public static void Run(
        Action<IServiceCollection, IConfiguration> register,
        Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        register(services, configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }
}
