namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>
/// Parsed representation of a /cdn-manifests/*.json file.
/// The initializer uses this to idempotently create buckets, configure CORS/lifecycle,
/// and pre-warm static assets at application startup.
/// </summary>
public sealed class EdgeManifest
{
    public required string ManifestId { get; init; }
    public required string Name { get; init; }
    public required string Bucket { get; init; }
    public List<EdgeManifestEntry> Entries { get; init; } = [];
    public CorsConfiguration? Cors { get; init; }
    public LifecycleConfiguration? Lifecycle { get; init; }
}

public sealed class EdgeManifestEntry
{
    /// <summary>Object key inside the bucket.</summary>
    public required string Key { get; init; }

    /// <summary>Absolute path on the local filesystem to the file to upload.</summary>
    public required string SourcePath { get; init; }

    /// <summary>Explicit MIME type. If null it is inferred from the file extension.</summary>
    public string? ContentType { get; init; }

    public Dictionary<string, string> Metadata { get; init; } = [];
    public bool PublicRead { get; init; } = false;
}

/// <summary>CORS policy applied to a bucket via the storage provider's API.</summary>
public sealed class CorsConfiguration
{
    public string[] AllowedOrigins { get; init; } = ["*"];
    public string[] AllowedMethods { get; init; } = ["GET", "HEAD"];
    public string[] AllowedHeaders { get; init; } = ["*"];
    public int MaxAgeSeconds { get; init; } = 3600;
}

/// <summary>Object lifecycle rules applied to a bucket (expiration, IA transition).</summary>
public sealed class LifecycleConfiguration
{
    /// <summary>Days after which objects expire and are deleted.</summary>
    public int ExpirationDays { get; init; } = 365;

    /// <summary>Days after which objects are transitioned to Infrequent Access storage class.</summary>
    public int TransitionToIADays { get; init; } = 30;

    public bool EnableExpiration { get; init; } = false;
    public bool EnableTransitionToIA { get; init; } = false;
}
