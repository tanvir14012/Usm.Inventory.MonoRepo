using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Low-level interface implemented by each concrete storage backend
/// (S3-compatible, Local FS, Azure Blob stub).
/// The <see cref="IStorageProviderEngine"/> wraps these with circuit-breaking and failover.
/// </summary>
public interface IStorageProvider
{
    string Name { get; }

    /// <summary>Lower value = higher precedence in routing and failover ordering.</summary>
    int Priority { get; }

    /// <summary>Geo-region tags this provider serves. Null means globally available.</summary>
    string[]? GeoRegions { get; }

    /// <summary>When true the provider may not be written to.</summary>
    bool IsReadOnly { get; }

    ValueTask<Stream?> GetObjectStreamAsync(string bucket, string key, CancellationToken ct = default);
    ValueTask<AssetMetadata?> GetMetadataAsync(string bucket, string key, CancellationToken ct = default);

    ValueTask PutObjectAsync(
        string bucket, string key, Stream content, string contentType,
        IDictionary<string, string>? metadata = null, CancellationToken ct = default);

    ValueTask DeleteObjectAsync(string bucket, string key, CancellationToken ct = default);
    ValueTask<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default);
    ValueTask EnsureBucketAsync(string bucket, CancellationToken ct = default);
    IAsyncEnumerable<string> ListObjectsAsync(string bucket, string prefix, CancellationToken ct = default);

    /// <summary>Returns a pre-signed (time-limited) download URL, or null if the provider does not support it.</summary>
    ValueTask<string?> GetPreSignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);

    ValueTask ConfigureBucketCorsAsync(string bucket, CorsConfiguration cors, CancellationToken ct = default);
    ValueTask ConfigureBucketLifecycleAsync(string bucket, LifecycleConfiguration lifecycle, CancellationToken ct = default);
}
