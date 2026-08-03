namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>Represents a multi-part upload session persisted in Redis.</summary>
public sealed class UploadSession
{
    public required string UploadId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long TotalSize { get; init; }
    public required int TotalChunks { get; init; }
    public required int ChunkSize { get; init; }
    public UploadStatus Status { get; set; } = UploadStatus.Pending;

    /// <summary>Indices of chunks that have been successfully written to storage.</summary>
    public List<int> CompletedChunks { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Final storage key after all chunks have been assembled.</summary>
    public string? FinalAssetKey { get; set; }

    /// <summary>Result of the background malware / virus scan hook (e.g. "clean", "infected:Trojan.Gen").</summary>
    public string? ScanStatus { get; set; }

    public string? ErrorMessage { get; set; }
}

public enum UploadStatus
{
    Pending,
    InProgress,
    Scanning,
    Completed,
    Failed,
    Aborted
}
