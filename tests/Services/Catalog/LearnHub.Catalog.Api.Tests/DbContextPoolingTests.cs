using LearnHub.Catalog.Domain.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

public sealed class DbContextPoolingTests
{
    [Fact]
    public async Task The_next_request_gets_the_same_context_with_nothing_of_the_previous_one()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new CatalogApiFactory();
        DbContextId firstLease;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            context.Courses.Add(Course.Create(
                Guid.NewGuid(), CourseTitle.Create("Pooled").Value, Money.Create(25m, "USD").Value));
            await context.SaveChangesAsync(cancellationToken);
            Assert.NotEmpty(context.ChangeTracker.Entries());
            firstLease = context.ContextId;
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            Assert.Equal(firstLease.InstanceId, context.ContextId.InstanceId);
            Assert.True(context.ContextId.Lease > firstLease.Lease);
            Assert.Empty(context.ChangeTracker.Entries());
        }
    }
}
