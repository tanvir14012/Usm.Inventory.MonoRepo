namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>Result returned to the caller after a single chunk has been accepted.</summary>
public sealed record UploadChunkResult
{
    public required string UploadId { get; init; }
    public required int ChunkIndex { get; init; }
    public required bool Success { get; init; }
    public string? ETag { get; init; }
    public string? Error { get; init; }
}
