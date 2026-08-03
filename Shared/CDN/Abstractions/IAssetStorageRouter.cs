using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Routes asset requests to the optimal <see cref="StorageEndpoint"/> based on
/// geo-region, priority, and current health state.  Used internally by strategies.
/// </summary>
public interface IAssetStorageRouter
{
    /// <summary>Returns the best available endpoint for the given asset and optional client region.</summary>
    ValueTask<StorageEndpoint> RouteAsync(string assetKey, string? clientRegion = null, CancellationToken ct = default);

    /// <summary>Returns the full failover chain (primary first) for a given asset key.</summary>
    ValueTask<IReadOnlyList<StorageEndpoint>> GetFailoverChainAsync(string assetKey, CancellationToken ct = default);

    /// <summary>Updates the live health state of a named provider (called by circuit-breaker callbacks).</summary>
    ValueTask ReportEndpointHealthAsync(string providerName, bool isHealthy, CancellationToken ct = default);
}
