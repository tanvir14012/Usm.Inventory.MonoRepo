namespace Usm.Shared.Infrastructure.CDN.Options;

/// <summary>Configuration for one logical storage backend (S3/MinIO/R2 or local FS).</summary>
public sealed class StorageProviderOptions
{
    /// <summary>Unique logical name used in circuit-breaker state and logging.</summary>
    public string Name { get; set; } = string.Empty;

    public StorageProviderType Type { get; set; } = StorageProviderType.S3Compatible;

    /// <summary>Service endpoint URL. Leave empty to use the AWS SDK regional default for AWS S3.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>AWS region identifier (e.g. "us-east-1"). Required for AWS S3; ignored for MinIO.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Default bucket name used when no bucket is explicitly specified by the caller.</summary>
    public string DefaultBucket { get; set; } = string.Empty;

    /// <summary>Force path-style URLs (required for MinIO and some S3-compatible APIs).</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>When true this provider is never written to (used as a read-only edge mirror).</summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>Lower value = higher priority in routing and failover ordering.</summary>
    public int Priority { get; set; } = 0;

    /// <summary>Geo-region tags this provider serves (e.g. ["eu-west","eu-central"]).  Null means global.</summary>
    public string[]? GeoRegions { get; set; }

    /// <summary>Root path for LocalFileSystem provider.</summary>
    public string? BasePath { get; set; }

    public bool UseHttps { get; set; } = true;
}

public enum StorageProviderType
{
    /// <summary>AWS S3, Cloudflare R2, or any MinIO-compatible endpoint.</summary>
    S3Compatible,
    LocalFileSystem,
    /// <summary>Azure Blob Storage (requires separate Azure.Storage.Blobs package – stub only).</summary>
    AzureBlob
}
