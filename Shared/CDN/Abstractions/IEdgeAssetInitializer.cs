namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Scans the /cdn-manifests directory and idempotently initialises buckets, CORS policies,
/// lifecycle rules, and pre-warmed static assets at application startup.
/// </summary>
public interface IEdgeAssetInitializer
{
    ValueTask InitializeAsync(CancellationToken ct = default);
}
