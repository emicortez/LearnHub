var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LearnHub_Catalog_Api>("catalog-api")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
