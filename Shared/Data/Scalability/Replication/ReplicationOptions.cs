namespace Usm.Shared.Data.Scalability.Replication;

public sealed class ReplicationOptions
{
    public const string SectionName = "Database:Replication";

    /// <summary>Enables or disables the read-replica routing interceptor.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Full Npgsql connection string pointing at the hot-standby / read replica.</summary>
    public string ReplicaConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum acceptable replication lag in milliseconds.
    /// When lag exceeds this threshold (if detectable), the strategy falls back to primary.
    /// </summary>
    public int MaxReplicaLagMs { get; set; } = 200;
}
