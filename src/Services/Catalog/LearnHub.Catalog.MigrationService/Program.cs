using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Runs once and exits: the Api waits for it, so a failed migration keeps the Api from starting on an old schema.
// A step of its own, because the Api should not need permission to change the schema.
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("catalog")));

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
