var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server, one database per service (ADR 0002). The data volume keeps the data across AppHost restarts;
// the generated password is saved in the AppHost user secrets, so it stays valid for the volume.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var catalogDb = postgres.AddDatabase("catalog");

var catalogMigrations = builder.AddProject<Projects.LearnHub_Catalog_MigrationService>("catalog-migrations")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

builder.AddProject<Projects.LearnHub_Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitForCompletion(catalogMigrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
