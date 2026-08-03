namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>Cached metadata record for a processed image or video variant stored in Redis.</summary>
public sealed record MediaVariant
{
    public required string OriginalKey { get; init; }

    /// <summary>Composite key used for variant data lookup: "{bucket}:{originalKey}:{cacheKeySuffix}".</summary>
    public required string VariantKey { get; init; }

    public required string ContentType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long Size { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? ETag { get; init; }
}
