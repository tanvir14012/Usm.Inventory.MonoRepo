namespace Usm.Shared.Data.Scalability.Partitioning;

/// <summary>Configuration for a partitioned PostgreSQL table backing a specific entity.</summary>
public sealed class PartitionOptions<TEntity> where TEntity : class
{
    public PartitionType Type { get; set; } = PartitionType.Range;

    /// <summary>
    /// Column(s) that form the partition key.
    /// Composite keys are expressed as multiple entries (e.g., ["tenant_id", "created_at"]).
    /// </summary>
    public string[] PartitionColumns { get; set; } = [];

    /// <summary>For HASH partitions: the modulus value (= number of child partition tables).</summary>
    public int HashModulus { get; set; } = 8;

    /// <summary>For RANGE partitions: how long each partition spans (e.g., 30 days → monthly).</summary>
    public TimeSpan? RangeInterval { get; set; }

    /// <summary>For LIST partitions: maps each discrete value to its child partition name.</summary>
    public Dictionary<string, string> ListValuePartitionMap { get; set; } = [];

    /// <summary>Override the parent table name (defaults to the EF Core table name for the entity).</summary>
    public string? ParentTableName { get; set; }

    /// <summary>Schema owning the parent and child partition tables.</summary>
    public string? Schema { get; set; }
}
