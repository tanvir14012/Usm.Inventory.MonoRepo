namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>Result produced by a CDN distribution strategy, consumed by the HTTP response layer.</summary>
public sealed class DistributionResult
{
    public required string StrategyUsed { get; init; }
    public required StorageEndpoint Endpoint { get; init; }

    /// <summary>Resolved asset metadata (may come from the origin-shield cache).</summary>
    public AssetMetadata? ResolvedMetadata { get; init; }

    /// <summary>True when the result was served entirely from a cache layer.</summary>
    public bool ServedFromCache { get; init; }

    /// <summary>True when the client's conditional request resulted in a 304 Not Modified response.</summary>
    public bool NotModified { get; init; }

    /// <summary>When set, the HTTP handler should issue a redirect instead of streaming content.</summary>
    public string? RedirectUrl { get; init; }

    /// <summary>Metadata for the processed image or video variant (set by EdgeProcessingStrategy).</summary>
    public MediaVariant? ProcessedVariant { get; init; }

    /// <summary>
    /// Pre-processed binary payload (e.g. a resized image produced by EdgeProcessingStrategy).
    /// When non-null the HTTP handler writes this directly instead of fetching from storage.
    /// </summary>
    public byte[]? InlineData { get; init; }

    /// <summary>Content-Type for InlineData. Required when InlineData is non-null.</summary>
    public string? InlineContentType { get; init; }
}
