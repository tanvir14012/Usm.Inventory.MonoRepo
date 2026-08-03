using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.Sharding;

/// <summary>
/// Default shard router that uses SHA-256 modulo hashing to distribute entities
/// uniformly across the configured <see cref="ShardNode"/> instances.
/// All operations are allocation-free on the hot path (stackalloc hash buffer).
/// </summary>
public sealed class DefaultShardRouter<TEntity>(
    IOptions<ShardingOptions> options,
    Func<TEntity, string> shardKeySelector) : IShardRouter<TEntity>
    where TEntity : class
{
    private readonly ShardingOptions _options = options.Value;
    private readonly Func<TEntity, string> _shardKeySelector = shardKeySelector;

    public string GetShardKey(TEntity entity) => _shardKeySelector(entity);

    public string ResolveConnectionString(string shardKey)
    {
        var index = ResolveShardIndex(shardKey);
        var node = _options.Nodes.Find(n => n.Index == index)
            ?? throw new InvalidOperationException(
                $"No shard node configured for index {index} " +
                $"(key='{shardKey}', totalShards={_options.TotalShards}).");
        return node.ConnectionString;
    }

    public int ResolveShardIndex(string shardKey)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(shardKey), hash);
        var magnitude = BinaryPrimitives.ReadUInt32LittleEndian(hash);
        return (int)(magnitude % (uint)_options.TotalShards);
    }

    public string ResolveTableSuffix(string shardKey)
    {
        var index = ResolveShardIndex(shardKey);
        var node = _options.Nodes.Find(n => n.Index == index);
        return node?.TableSuffix ?? $"_{index:D2}";
    }
}

/// <summary>
/// Ambient context carrying the active shard routing decision set by
/// <c>ShardingStrategy&lt;TEntity&gt;</c>. Consumed by custom connection factories
/// or <c>IDbConnectionInterceptor</c> implementations.
/// </summary>
public static class ShardContext
{
    private static readonly AsyncLocal<string?> _shardKey = new();
    private static readonly AsyncLocal<string?> _connectionString = new();

    public static string? CurrentShardKey => _shardKey.Value;
    public static string? CurrentConnectionString => _connectionString.Value;

    public static void SetCurrentShard(string shardKey, string connectionString)
    {
        _shardKey.Value = shardKey;
        _connectionString.Value = connectionString;
    }

    public static void Clear()
    {
        _shardKey.Value = null;
        _connectionString.Value = null;
    }
}
