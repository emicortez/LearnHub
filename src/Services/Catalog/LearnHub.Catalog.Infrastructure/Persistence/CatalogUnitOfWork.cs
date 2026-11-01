using LearnHub.Catalog.Application;
using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Persistence;

/// <summary>
/// One call to <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>, which is one transaction, followed by the
/// dispatch of the events it committed.
/// </summary>
public sealed class CatalogUnitOfWork(CatalogDbContext context, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .ToList();

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CourseConfiguration.UniqueSlug,
        })
        {
            // Only after the insert fails: asking first would let two requests both see the slug as free.
            return CourseErrors.SlugTaken;
        }

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        // Cleared before dispatching, so a later save in this context cannot dispatch them a second time.
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

        // The commit already happened, so the caller's cancellation no longer applies to announcing it.
        await dispatcher.DispatchAsync(domainEvents, CancellationToken.None).ConfigureAwait(false);

        return Result.Success();
    }
}
