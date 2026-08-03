namespace Usm.Shared.Data.Scalability.Abstractions;

/// <summary>Routes entities to horizontal shard nodes based on a computed shard key.</summary>
public interface IShardRouter<TEntity> where TEntity : class
{
    /// <summary>Computes the shard key for the entity (e.g., tenant ID, region code).</summary>
    string GetShardKey(TEntity entity);

    /// <summary>Returns the connection string for the shard that owns the given key.</summary>
    string ResolveConnectionString(string shardKey);

    /// <summary>Returns the zero-based shard index in the range [0, TotalShards).</summary>
    int ResolveShardIndex(string shardKey);

    /// <summary>Returns the table-name suffix for the shard (e.g., "_03") for table-level sharding.</summary>
    string ResolveTableSuffix(string shardKey);
}
