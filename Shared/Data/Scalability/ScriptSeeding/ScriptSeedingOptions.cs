namespace Usm.Shared.Data.Scalability.ScriptSeeding;

public sealed class ScriptSeedingOptions
{
    public const string SectionName = "Database:ScriptSeeding";

    /// <summary>
    /// Npgsql connection string used for script execution.
    /// Should typically point at a migration/admin role with DDL privileges.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Root directory containing the .sql script files.
    /// Relative paths are resolved from the application's base directory.
    /// </summary>
    public string ScriptDirectory { get; set; } = "scripts";

    /// <summary>
    /// Name of the tracking table (created automatically on first run).
    /// </summary>
    public string TrackingTable { get; set; } = "__script_migrations";

    /// <summary>Schema that owns the tracking table.</summary>
    public string TrackingSchema { get; set; } = "public";

    /// <summary>
    /// Sub-folder execution order. Scripts inside each folder are sorted numerically by
    /// their filename prefix (e.g., <c>001_create_types.sql</c>).
    /// Folders not listed here are executed last in filesystem order.
    /// </summary>
    public string[] ScriptFolderOrder { get; set; } =
        ["seeds", "functions", "procedures", "views", "materialized_views", "schedulers"];

    /// <summary>
    /// When <c>true</c>, each script is executed inside an explicit transaction that is
    /// committed on success or rolled back on failure.
    /// Set to <c>false</c> for DDL scripts that cannot run inside a transaction (rare).
    /// </summary>
    public bool UseTransactionPerScript { get; set; } = true;

    /// <summary>Per-command timeout in seconds.</summary>
    public int CommandTimeoutSeconds { get; set; } = 300;
}
