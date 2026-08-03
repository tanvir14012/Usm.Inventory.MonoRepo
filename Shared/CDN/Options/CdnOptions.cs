namespace Usm.Shared.Infrastructure.CDN.Options;

/// <summary>Top-level CDN configuration bound from appsettings "CDN" section.</summary>
public sealed class CdnOptions
{
    public const string SectionName = "CDN";

    /// <summary>Public base URL of the CDN (e.g. https://cdn.example.com).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Directory scanned for JSON asset manifests at startup.</summary>
    public string ManifestDirectory { get; set; } = "/cdn-manifests";

    /// <summary>Default distribution strategy when no strategy-specific condition is satisfied.</summary>
    public DistributionStrategyType DefaultStrategy { get; set; } = DistributionStrategyType.OriginShield;

    /// <summary>Ordered list of storage provider configurations (lower Priority index = higher precedence).</summary>
    public StorageProviderOptions[] StorageProviders { get; set; } = [];

    /// <summary>Nginx secure-link signing options.</summary>
    public NginxSecureLinkOptions SecureLink { get; set; } = new();

    /// <summary>Image/video processing pipeline options.</summary>
    public MediaProcessingOptions MediaProcessing { get; set; } = new();

    /// <summary>Number of consecutive failures before a storage provider circuit opens.</summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>How long the circuit stays open before a half-open probe is allowed.</summary>
    public TimeSpan CircuitBreakerOpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>When true, the EdgeProcessing strategy performs on-the-fly image transformations.</summary>
    public bool EnableEdgeProcessing { get; set; } = true;

    /// <summary>MIME type patterns permitted for upload (supports wildcard e.g. "image/*").</summary>
    public string[] AllowedUploadMimeTypes { get; set; } = ["image/*", "video/*", "application/octet-stream"];

    /// <summary>Maximum single-upload size in bytes (default 5 GB).</summary>
    public long MaxUploadSizeBytes { get; set; } = 5L * 1024 * 1024 * 1024;

    /// <summary>Multipart chunk size in bytes (default 5 MB – minimum required by S3).</summary>
    public int ChunkSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Redis connection string for pub/sub cache invalidation. Falls back to REDIS_CONNECTION_STRING env var.</summary>
    public string? RedisConnectionString { get; set; }
}

public enum DistributionStrategyType
{
    Regional,
    LoadBalanced,
    OriginShield,
    EdgeProcessing
}
