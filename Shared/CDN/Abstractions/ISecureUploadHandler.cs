using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Handles authenticated chunked file uploads with background malware-scan hooks
/// and a status-polling endpoint.
/// </summary>
public interface ISecureUploadHandler
{
    /// <summary>Creates a new upload session and returns its metadata.</summary>
    ValueTask<UploadSession> InitiateUploadAsync(
        string fileName, string contentType, long fileSize, CancellationToken ct = default);

    /// <summary>Accepts a single chunk of a multi-part upload.</summary>
    ValueTask<UploadChunkResult> UploadChunkAsync(
        string uploadId, int chunkIndex, Stream chunk, CancellationToken ct = default);

    /// <summary>
    /// Assembles all chunks, triggers the scan hook, and returns the final asset metadata.
    /// Throws if any chunk is missing.
    /// </summary>
    ValueTask<AssetMetadata> CompleteUploadAsync(string uploadId, CancellationToken ct = default);

    /// <summary>Returns the current status of an upload session, or null if it has expired.</summary>
    ValueTask<UploadSession?> GetUploadStatusAsync(string uploadId, CancellationToken ct = default);

    /// <summary>Aborts an in-progress upload and cleans up any stored chunks.</summary>
    ValueTask AbortUploadAsync(string uploadId, CancellationToken ct = default);
}
