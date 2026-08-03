namespace Usm.Shared.Data.Scalability.Sharding;

public sealed class ShardingOptions
{
    public const string SectionName = "Database:Sharding";

    public bool IsEnabled { get; set; } = true;

    /// <summary>Total number of logical shards (must match the number of configured <see cref="Nodes"/>).</summary>
    public int TotalShards { get; set; } = 4;

    /// <summary>One descriptor per shard node, indexed from 0.</summary>
    public List<ShardNode> Nodes { get; set; } = [];
}

public sealed class ShardNode
{
    /// <summary>Zero-based index identifying this shard (must be in [0, TotalShards)).</summary>
    public int Index { get; set; }

    /// <summary>Full Npgsql connection string for this shard's database instance.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional table-name suffix used in table-level sharding (e.g., "_03" → "orders_03").
    /// Defaults to <c>$"_{Index:D2}"</c> when not explicitly set.
    /// </summary>
    public string? TableSuffix { get; set; }
}
