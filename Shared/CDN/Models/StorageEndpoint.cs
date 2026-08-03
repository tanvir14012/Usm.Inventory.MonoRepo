namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>Represents a resolved storage backend endpoint returned by the distribution strategy.</summary>
public sealed record StorageEndpoint
{
    public required string ProviderName { get; init; }

    /// <summary>Service URL base (e.g. https://s3.us-east-1.amazonaws.com or https://minio.internal).</summary>
    public required string BaseUrl { get; init; }

    public required string Bucket { get; init; }
    public required string Region { get; init; }

    /// <summary>Lower = higher priority in failover chains.</summary>
    public int Priority { get; init; }

    /// <summary>Set to false by the circuit-breaker or health-check system.</summary>
    public bool IsHealthy { get; init; } = true;

    /// <summary>Geo-region tag used by the regional sharding strategy.</summary>
    public string? GeoRegion { get; init; }

    /// <summary>Measured round-trip latency in ms (used by latency-based routing).</summary>
    public double? LatencyMs { get; init; }
}
