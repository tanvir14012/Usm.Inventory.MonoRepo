namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>
/// Holds the output of an image processing operation.
/// The byte array is stored directly in Redis as a variant payload.
/// </summary>
public sealed class ProcessedMedia
{
    public required byte[] Data { get; init; }
    public required string ContentType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Cache key that uniquely identifies this variant (derived from MediaProcessingRequest).</summary>
    public string? CacheKey { get; init; }

    /// <summary>True when this result was retrieved from the Redis variant cache.</summary>
    public bool WasCached { get; init; }
}
