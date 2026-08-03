using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Storage;

/// <summary>
/// IStorageProvider implementation backed by the local file system.
/// Intended for development, single-node edge deployments, and testing.
/// Pre-signed URLs are not supported (returns null).
/// </summary>
internal sealed class LocalFileSystemStorageProvider(
    StorageProviderOptions options,
    ILogger<LocalFileSystemStorageProvider> logger) : IStorageProvider
{
    private readonly string _basePath = string.IsNullOrEmpty(options.BasePath)
        ? Path.Combine(Path.GetTempPath(), "usm-cdn-storage")
        : options.BasePath;

    public string Name => options.Name;
    public int Priority => options.Priority;
    public string[]? GeoRegions => options.GeoRegions;
    public bool IsReadOnly => options.IsReadOnly;

    private string ObjectPath(string bucket, string key)
        => Path.Combine(_basePath, bucket, key.Replace('/', Path.DirectorySeparatorChar));

    // ── Read operations ──────────────────────────────────────────────────────

    public ValueTask<Stream?> GetObjectStreamAsync(string bucket, string key, CancellationToken ct = default)
    {
        var path = ObjectPath(bucket, key);
        if (!File.Exists(path))
            return ValueTask.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 65_536, useAsync: true);
        return ValueTask.FromResult<Stream?>(stream);
    }

    public ValueTask<AssetMetadata?> GetMetadataAsync(string bucket, string key, CancellationToken ct = default)
    {
        var path = ObjectPath(bucket, key);
        if (!File.Exists(path))
            return ValueTask.FromResult<AssetMetadata?>(null);

        var info = new FileInfo(path);
        var userMeta = ReadSidecarMeta(path);
        var contentType = userMeta.TryGetValue("Content-Type", out var ct2)
            ? ct2
            : "application/octet-stream";

        return ValueTask.FromResult<AssetMetadata?>(new AssetMetadata
        {
            Key = key,
            Bucket = bucket,
            ContentType = contentType,
            Size = info.Length,
            LastModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            ETag = ComputeETag(info),
            StorageProvider = Name,
            Metadata = userMeta
        });
    }

    // ── Write operations ─────────────────────────────────────────────────────

    public async ValueTask PutObjectAsync(
        string bucket, string key, Stream content, string contentType,
        IDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var path = ObjectPath(bucket, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 65_536, useAsync: true);
        await content.CopyToAsync(file, ct).ConfigureAwait(false);

        var meta = new Dictionary<string, string>(StringComparer.Ordinal);
        if (metadata is not null)
            foreach (var (k, v) in metadata)
                meta[k] = v;
        meta["Content-Type"] = contentType;
        await WriteSidecarMetaAsync(path, meta, ct).ConfigureAwait(false);
    }

    public ValueTask DeleteObjectAsync(string bucket, string key, CancellationToken ct = default)
    {
        var path = ObjectPath(bucket, key);
        if (File.Exists(path))
            File.Delete(path);
        var meta = path + ".meta";
        if (File.Exists(meta))
            File.Delete(meta);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default)
        => ValueTask.FromResult(File.Exists(ObjectPath(bucket, key)));

    // ── Bucket management ────────────────────────────────────────────────────

    public ValueTask EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.Combine(_basePath, bucket));
        logger.LogDebug("[{Provider}] Ensured local bucket directory for '{Bucket}'", Name, bucket);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<string> ListObjectsAsync(
        string bucket, string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var bucketDir = Path.Combine(_basePath, bucket);
        if (!Directory.Exists(bucketDir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(bucketDir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = Path.GetRelativePath(bucketDir, file)
                .Replace(Path.DirectorySeparatorChar, '/');

            if (string.IsNullOrEmpty(prefix) ||
                relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                yield return relative;
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    // ── Not supported ────────────────────────────────────────────────────────

    public ValueTask<string?> GetPreSignedUrlAsync(
        string bucket, string key, TimeSpan expiry, CancellationToken ct = default)
        => ValueTask.FromResult<string?>(null);

    public ValueTask ConfigureBucketCorsAsync(
        string bucket, CorsConfiguration cors, CancellationToken ct = default)
        => ValueTask.CompletedTask;

    public ValueTask ConfigureBucketLifecycleAsync(
        string bucket, LifecycleConfiguration lifecycle, CancellationToken ct = default)
        => ValueTask.CompletedTask;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ComputeETag(FileInfo info)
        => $"\"{info.LastWriteTimeUtc.Ticks:X16}-{info.Length:X}\"";

    private static Dictionary<string, string> ReadSidecarMeta(string objectPath)
    {
        var metaPath = objectPath + ".meta";
        if (!File.Exists(metaPath))
            return [];

        return File.ReadAllLines(metaPath)
            .Select(l => l.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1], StringComparer.Ordinal);
    }

    private static async ValueTask WriteSidecarMetaAsync(
        string objectPath, Dictionary<string, string> meta, CancellationToken ct)
    {
        var lines = meta.Select(kv => $"{kv.Key}={kv.Value}");
        await File.WriteAllLinesAsync(objectPath + ".meta", lines, ct).ConfigureAwait(false);
    }
}
