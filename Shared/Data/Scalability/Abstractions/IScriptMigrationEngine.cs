namespace Usm.Shared.Data.Scalability.Abstractions;

/// <summary>
/// Idempotent SQL script execution engine.
/// Scans configured directories for .sql files, orders them by folder and numeric prefix,
/// and applies only those not yet recorded in the tracking table.
/// </summary>
public interface IScriptMigrationEngine
{
    /// <summary>Executes all pending scripts in order.</summary>
    ValueTask ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns names of scripts that have not yet been applied.</summary>
    ValueTask<IReadOnlyList<string>> GetPendingScriptsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns names of scripts already recorded in the tracking table.</summary>
    ValueTask<IReadOnlyList<string>> GetAppliedScriptsAsync(CancellationToken cancellationToken = default);
}
