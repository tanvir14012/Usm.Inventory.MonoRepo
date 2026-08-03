namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>Immutable metadata record for a CDN-managed asset, cached in Redis to avoid repeated HEAD requests.</summary>
public sealed record AssetMetadata
{
    public required string Key { get; init; }
    public required string Bucket { get; init; }
    public required string ContentType { get; init; }
    public required long Size { get; init; }
    public required DateTimeOffset LastModified { get; init; }
    /// <summary>ETag without surrounding quotes.</summary>
    public string? ETag { get; init; }
    public string? ContentHash { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = [];
    public string? StorageProvider { get; init; }
    public string? Region { get; init; }
    public bool IsPublic { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
