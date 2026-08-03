using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Usm.Shared.Data.Scalability.Sharding;

/// <summary>
/// Immutable value type representing a computed shard key.
/// Uses SHA-256 for a deterministic, collision-resistant distribution.
/// </summary>
public readonly record struct ShardKey(string Value)
{
    /// <summary>Maps this key to a zero-based shard index in the range [0, <paramref name="totalShards"/>).</summary>
    public int ToIndex(int totalShards)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(totalShards, 1);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(Value), hash);
        var magnitude = BinaryPrimitives.ReadUInt32LittleEndian(hash);
        return (int)(magnitude % (uint)totalShards);
    }

    public override string ToString() => Value;
    public static implicit operator string(ShardKey key) => key.Value;
    public static implicit operator ShardKey(string value) => new(value);
}
