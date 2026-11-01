using System.Collections.Concurrent;
using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Keeps every query EF Core sends, so a test can count them (an N+1 shows up as one command per row)
/// and replay the real statement under EXPLAIN.
/// </summary>
public sealed class CommandRecorder : DbCommandInterceptor
{
    private readonly ConcurrentQueue<RecordedCommand> _commands = new();

    public IReadOnlyList<RecordedCommand> Commands => [.. _commands];

    public void Clear() => _commands.Clear();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }

    private void Record(DbCommand command) => _commands.Enqueue(new(
        command.CommandText,
        [.. command.Parameters.Cast<NpgsqlParameter>().Select(parameter => parameter.Clone())]));
}

public sealed record RecordedCommand(string Text, IReadOnlyList<NpgsqlParameter> Parameters);
