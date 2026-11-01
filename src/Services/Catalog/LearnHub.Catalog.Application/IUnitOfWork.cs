using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application;

/// <summary>
/// Commits every change made during the current operation in one transaction. Handlers call it, because they know
/// where the operation ends; repositories never do, so one operation cannot be split into two commits.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits the pending changes, then dispatches the domain events they raised. A database rule that is a
    /// business rule, such as a taken slug, comes back as a failure; nothing is committed or dispatched then.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
