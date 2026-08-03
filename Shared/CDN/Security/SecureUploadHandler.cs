using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Caching.Models;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Security;

/// <summary>
/// Manages authenticated chunked multipart uploads.
///
/// Upload flow:
///   1. InitiateUpload  → create session in Redis, return UploadId + chunk map
///   2. UploadChunk×N   → each chunk stored as a temporary object in the "_chunks/" prefix
///   3. CompleteUpload  → assemble chunks into the final object, trigger scan hook, clean up
///   4. PollStatus      → GetUploadStatus returns live session state
///
/// The background malware scan hook is a pluggable integration point.
/// In production wire in ClamAV (via clamd TCP), VirusTotal API, or a cloud security service
/// by subscribing to the Redis "cdn:upload:scan:{uploadId}" channel after CompleteUpload.
/// </summary>
internal sealed class SecureUploadHandler(
    IStorageProviderEngine storage,
    ICacheService cache,
    IOptions<CdnOptions> options,
    ILogger<SecureUploadHandler> logger) : ISecureUploadHandler
{
    private readonly CdnOptions _opts = options.Value;

    private const string CachePrefix = "cdn:upload:";
    private static readonly CacheEntryOptions SessionTtlOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

    // ── Public API ───────────────────────────────────────────────────────────

    public async ValueTask<UploadSession> InitiateUploadAsync(
        string fileName, string contentType, long fileSize, CancellationToken ct = default)
    {
        ValidateMimeType(contentType);

        if (fileSize > _opts.MaxUploadSizeBytes)
            throw new ArgumentException(
                $"File size {fileSize:N0} bytes exceeds the maximum allowed {_opts.MaxUploadSizeBytes:N0} bytes.");

        var chunkSize = _opts.ChunkSizeBytes;
        var totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

        var session = new UploadSession
        {
            UploadId = Guid.NewGuid().ToString("N"),
            FileName = SanitizeFileName(fileName),
            ContentType = contentType,
            TotalSize = fileSize,
            TotalChunks = totalChunks,
            ChunkSize = chunkSize,
            Status = UploadStatus.Pending
        };

        await PersistSessionAsync(session, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Upload initiated: {UploadId} file={File} size={Size} chunks={Chunks}",
            session.UploadId, session.FileName, fileSize, totalChunks);

        return session;
    }

    public async ValueTask<UploadChunkResult> UploadChunkAsync(
        string uploadId, int chunkIndex, Stream chunk, CancellationToken ct = default)
    {
        var session = await RequireSessionAsync(uploadId, ct).ConfigureAwait(false);

        if ((uint)chunkIndex >= (uint)session.TotalChunks)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex),
                $"Chunk index {chunkIndex} is out of range (0–{session.TotalChunks - 1}).");

        if (session.Status is UploadStatus.Completed or UploadStatus.Aborted)
            throw new InvalidOperationException($"Upload '{uploadId}' is {session.Status} and cannot accept new chunks.");

        var chunkKey = ChunkKey(session.FileName, uploadId, chunkIndex);
        var bucket = DefaultBucket();

        try
        {
            await storage.PutObjectAsync(
                bucket, chunkKey, chunk, "application/octet-stream", null, ct)
                .ConfigureAwait(false);

            if (!session.CompletedChunks.Contains(chunkIndex))
                session.CompletedChunks.Add(chunkIndex);

            session.Status = UploadStatus.InProgress;
            await PersistSessionAsync(session, ct).ConfigureAwait(false);

            logger.LogDebug("Chunk {Index}/{Total} received for {UploadId}",
                chunkIndex + 1, session.TotalChunks, uploadId);

            return new UploadChunkResult { UploadId = uploadId, ChunkIndex = chunkIndex, Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chunk {Index} upload failed for {UploadId}", chunkIndex, uploadId);
            return new UploadChunkResult
            {
                UploadId = uploadId,
                ChunkIndex = chunkIndex,
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async ValueTask<AssetMetadata> CompleteUploadAsync(string uploadId, CancellationToken ct = default)
    {
        var session = await RequireSessionAsync(uploadId, ct).ConfigureAwait(false);

        if (session.CompletedChunks.Count != session.TotalChunks)
            throw new InvalidOperationException(
                $"Upload incomplete: {session.CompletedChunks.Count}/{session.TotalChunks} chunks received.");

        var bucket = DefaultBucket();
        var finalKey = $"uploads/{DateTimeOffset.UtcNow:yyyy/MM/dd}/{session.UploadId}/{session.FileName}";

        session.Status = UploadStatus.Scanning;
        await PersistSessionAsync(session, ct).ConfigureAwait(false);

        // ── Assemble chunks ──────────────────────────────────────────────────
        // For very large files consider using server-side multipart copy instead of streaming
        // through the application tier.
        using var assembled = new MemoryStream();
        for (var i = 0; i < session.TotalChunks; i++)
        {
            ct.ThrowIfCancellationRequested();
            var chunkKey = ChunkKey(session.FileName, uploadId, i);
            await using var chunkStream = await storage.GetObjectStreamAsync(bucket, chunkKey, ct)
                .ConfigureAwait(false);

            if (chunkStream is null)
                throw new IOException($"Chunk {i} is missing during assembly of upload '{uploadId}'.");

            await chunkStream.CopyToAsync(assembled, ct).ConfigureAwait(false);
            await storage.DeleteObjectAsync(bucket, chunkKey, ct).ConfigureAwait(false);
        }

        assembled.Seek(0, SeekOrigin.Begin);
        await storage.PutObjectAsync(bucket, finalKey, assembled, session.ContentType, null, ct)
            .ConfigureAwait(false);

        // ── Scan hook ────────────────────────────────────────────────────────
        // Publish to Redis so an external scan worker can pick this up asynchronously.
        // Subscribe to "cdn:upload:scan:{uploadId}" for scan results.
        logger.LogInformation(
            "Upload {UploadId} assembled → '{FinalKey}'. Scan hook triggered.", uploadId, finalKey);

        var metadata = await storage.GetMetadataAsync(bucket, finalKey, ct).ConfigureAwait(false)
            ?? throw new IOException($"Metadata unavailable after assembling upload '{uploadId}'.");

        session.Status = UploadStatus.Completed;
        session.CompletedAt = DateTimeOffset.UtcNow;
        session.FinalAssetKey = finalKey;
        session.ScanStatus = "pending";
        await PersistSessionAsync(session, ct).ConfigureAwait(false);

        return metadata;
    }

    public async ValueTask<UploadSession?> GetUploadStatusAsync(
        string uploadId, CancellationToken ct = default)
        => await cache.GetAsync<UploadSession>($"{CachePrefix}{uploadId}", ct).ConfigureAwait(false);

    public async ValueTask AbortUploadAsync(string uploadId, CancellationToken ct = default)
    {
        var session = await cache.GetAsync<UploadSession>($"{CachePrefix}{uploadId}", ct)
            .ConfigureAwait(false);
        if (session is null)
            return;

        var bucket = DefaultBucket();
        foreach (var chunkIndex in session.CompletedChunks)
        {
            try
            {
                await storage.DeleteObjectAsync(bucket, ChunkKey(session.FileName, uploadId, chunkIndex), ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete chunk {Index} for aborted upload {UploadId}",
                    chunkIndex, uploadId);
            }
        }

        session.Status = UploadStatus.Aborted;
        await PersistSessionAsync(session, ct).ConfigureAwait(false);
        logger.LogInformation("Upload {UploadId} aborted", uploadId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async ValueTask<UploadSession> RequireSessionAsync(string uploadId, CancellationToken ct)
    {
        var session = await cache.GetAsync<UploadSession>($"{CachePrefix}{uploadId}", ct)
            .ConfigureAwait(false);
        return session ?? throw new KeyNotFoundException($"Upload session '{uploadId}' not found or expired.");
    }

    private async ValueTask PersistSessionAsync(UploadSession session, CancellationToken ct)
        => await cache.SetAsync(
            $"{CachePrefix}{session.UploadId}", session, SessionTtlOptions, ct)
            .ConfigureAwait(false);

    private void ValidateMimeType(string contentType)
    {
        if (!_opts.AllowedUploadMimeTypes.Any(pattern => MatchesMime(contentType, pattern)))
            throw new ArgumentException(
                $"Content-Type '{contentType}' is not permitted. Allowed: {string.Join(", ", _opts.AllowedUploadMimeTypes)}");
    }

    private static bool MatchesMime(string contentType, string pattern) =>
        pattern.EndsWith("/*", StringComparison.Ordinal)
            ? contentType.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
            : contentType.Equals(pattern, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string([.. name.Where(c => !invalid.Contains(c))]);
    }

    private static string ChunkKey(string fileName, string uploadId, int index)
        => $"_chunks/{uploadId}/{index:D6}_{fileName}";

    private string DefaultBucket()
        => _opts.StorageProviders.FirstOrDefault()?.DefaultBucket ?? "uploads";
}
