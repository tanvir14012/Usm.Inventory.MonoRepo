using System.Security.Cryptography;
using Usm.Shared.Algorithms.Collections.BloomFilter.Abstractions;

namespace Usm.Shared.Algorithms.Collections.BloomFilter;

/// <summary>
/// Generic Bloom filter using double hashing.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class BloomFilter<T> : IBloomFilter<T>
    where T : notnull
{
    private readonly ulong[] _bits;
    private readonly int _bitCount;
    private readonly int _hashCount;
    private readonly IEqualityComparer<T> _comparer;
    private readonly object _gate = new();
    private int _count;

    /// <summary>Initializes a new Bloom filter.</summary>
    public BloomFilter(int expectedItemCount, double falsePositiveRate, IEqualityComparer<T> comparer)
    {
        if (expectedItemCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedItemCount));
        if (falsePositiveRate <= 0 || falsePositiveRate >= 1)
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));

        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        var m = Math.Ceiling(-(expectedItemCount * Math.Log(falsePositiveRate)) / Math.Log(2) / Math.Log(2));
        _bitCount = Math.Max(64, (int)m);
        _hashCount = Math.Max(1, (int)Math.Round((_bitCount / (double)expectedItemCount) * Math.Log(2)));
        _bits = new ulong[(_bitCount + 63) / 64];
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_gate)
                return _count;
        }
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        lock (_gate)
        {
            SetBits(item);
            _count++;
        }
    }

    /// <inheritdoc />
    public ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Add(item);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public bool MightContain(T item)
    {
        lock (_gate)
            return CheckBits(item);
    }

    /// <inheritdoc />
    public ValueTask<bool> MightContainAsync(T item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(MightContain(item));
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            Array.Clear(_bits);
            _count = 0;
        }
    }

    private void SetBits(T item)
    {
        var hashes = GetHashes(item);
        for (var i = 0; i < _hashCount; i++)
            SetBit(hashes[i]);
    }

    private bool CheckBits(T item)
    {
        var hashes = GetHashes(item);
        for (var i = 0; i < _hashCount; i++)
        {
            if (!GetBit(hashes[i]))
                return false;
        }

        return true;
    }

    private void SetBit(ulong hash)
    {
        var bitIndex = (int)(hash % (ulong)_bitCount);
        var slot = bitIndex / 64;
        var offset = bitIndex % 64;
        _bits[slot] |= 1UL << offset;
    }

    private bool GetBit(ulong hash)
    {
        var bitIndex = (int)(hash % (ulong)_bitCount);
        var slot = bitIndex / 64;
        var offset = bitIndex % 64;
        return (_bits[slot] & (1UL << offset)) != 0;
    }

    private ulong[] GetHashes(T item)
    {
        var hash1 = (ulong)_comparer.GetHashCode(item);
        var hash2 = Mix(hash1);
        var hashes = new ulong[_hashCount];
        for (var i = 0; i < _hashCount; i++)
            hashes[i] = hash1 + (ulong)i * hash2;

        return hashes;
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 33;
        value *= 0xff51afd7ed558ccdUL;
        value ^= value >> 33;
        value *= 0xc4ceb9fe1a85ec53UL;
        value ^= value >> 33;
        return value | 1UL;
    }
}
