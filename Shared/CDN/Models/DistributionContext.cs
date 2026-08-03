namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>
/// Per-request context passed through the CDN distribution strategy pipeline.
/// Built by the HTTP handler layer from incoming request headers and query parameters.
/// </summary>
public sealed class DistributionContext
{
    public required string AssetKey { get; init; }
    public required string Bucket { get; init; }

    /// <summary>Client IP resolved from X-Forwarded-For or RemoteIpAddress.</summary>
    public string? ClientIp { get; init; }

    /// <summary>Geo-region identifier derived from CF-IPCountry / CloudFront-Viewer-Country headers or GeoIP lookup.</summary>
    public string? ClientRegion { get; init; }

    /// <summary>Value of the HTTP Accept header (e.g. "image/webp,image/avif,*/*").</summary>
    public string? AcceptHeader { get; init; }

    /// <summary>Value of the HTTP Range header (e.g. "bytes=0-1048575").</summary>
    public string? RangeHeader { get; init; }

    /// <summary>Value of the If-None-Match header for conditional GET support.</summary>
    public string? IfNoneMatch { get; init; }

    /// <summary>Value of the If-Modified-Since header for conditional GET support.</summary>
    public string? IfModifiedSince { get; init; }

    /// <summary>Pre-populated metadata from an upstream cache lookup (avoids duplicate HEAD calls).</summary>
    public AssetMetadata? CachedMetadata { get; init; }

    /// <summary>Parsed query parameters (w, h, format, q, etc.) for edge processing.</summary>
    public IDictionary<string, string> QueryParams { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Populated when the request includes image-transformation parameters.</summary>
    public MediaProcessingRequest? ProcessingRequest { get; init; }
}
