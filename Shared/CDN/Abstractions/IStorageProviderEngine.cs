using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// High-level storage facade that wraps multiple <see cref="IStorageProvider"/> backends
/// with automatic circuit-breaking, failover, and provider selection.
/// Inject this interface in application code; use <see cref="IStorageProvider"/> only in
/// infrastructure-layer components that need per-provider control.
/// </summary>
public interface IStorageProviderEngine
{
    ValueTask<Stream?> GetObjectStreamAsync(string bucket, string key, CancellationToken ct = default);
    ValueTask<AssetMetadata?> GetMetadataAsync(string bucket, string key, CancellationToken ct = default);

    ValueTask PutObjectAsync(
        string bucket, string key, Stream content, string contentType,
        IDictionary<string, string>? metadata = null, CancellationToken ct = default);

    ValueTask DeleteObjectAsync(string bucket, string key, CancellationToken ct = default);
    ValueTask<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default);
    ValueTask EnsureBucketAsync(string bucket, CancellationToken ct = default);
    IAsyncEnumerable<string> ListObjectsAsync(string bucket, string prefix, CancellationToken ct = default);
    ValueTask<string?> GetPreSignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);
}
