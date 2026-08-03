using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Strategies;

/// <summary>
/// Routes requests to a geo-specific storage provider when client region headers are present,
/// or falls back to consistent-hash sharding on the asset key to ensure the same asset
/// always maps to the same provider replica (reducing cache churn).
///
/// Priority: 10 (evaluated after EdgeProcessing and OriginShield).
/// Activates when: geo-region headers are present OR any provider has GeoRegions configured.
/// </summary>
internal sealed class RegionalShardingStrategy(
    IStorageProviderEngine storageEngine,
    IOptions<CdnOptions> options,
    ILogger<RegionalShardingStrategy> logger) : ICdnDistributionStrategy
{
    private readonly CdnOptions _opts = options.Value;

    public string Name => "RegionalSharding";
    public int Priority => 10;

    public bool CanHandle(DistributionContext context)
        => context.ClientRegion is not null
           || _opts.StorageProviders.Any(p => p.GeoRegions?.Length > 0);

    public async ValueTask<DistributionResult> ExecuteAsync(
        DistributionContext context, CancellationToken cancellationToken = default)
    {
        var providerName = SelectProvider(context);
        var providerOpts = _opts.StorageProviders
            .FirstOrDefault(p => p.Name == providerName)
            ?? _opts.StorageProviders.FirstOrDefault()
            ?? throw new InvalidOperationException("No storage providers configured.");

        logger.LogDebug("[{Strategy}] Selected '{Provider}' for region '{Region}' / key '{Key}'",
            Name, providerName, context.ClientRegion, context.AssetKey);

        var metadata = await storageEngine
            .GetMetadataAsync(context.Bucket, context.AssetKey, cancellationToken)
            .ConfigureAwait(false);

        return new DistributionResult
        {
            StrategyUsed = Name,
            Endpoint = BuildEndpoint(providerOpts, context.ClientRegion),
            ResolvedMetadata = metadata
        };
    }

    private string SelectProvider(DistributionContext context)
    {
        // 1. Exact geo-region match
        if (context.ClientRegion is not null)
        {
            var geo = _opts.StorageProviders.FirstOrDefault(p =>
                p.GeoRegions?.Contains(context.ClientRegion, StringComparer.OrdinalIgnoreCase) == true);
            if (geo is not null) return geo.Name;
        }

        // 2. Consistent-hash fallback: deterministic shard assignment per asset key
        var writable = _opts.StorageProviders.Where(p => !p.IsReadOnly).OrderBy(p => p.Priority).ToArray();
        if (writable.Length == 0)
            return _opts.StorageProviders.First().Name;

        // SHA-256 of the key gives uniform distribution; take 4 bytes as unsigned int
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(context.AssetKey));
        var bucket = (int)(BitConverter.ToUInt32(hashBytes, 0) % (uint)writable.Length);
        return writable[bucket].Name;
    }

    private static StorageEndpoint BuildEndpoint(StorageProviderOptions p, string? geoRegion) =>
        new()
        {
            ProviderName = p.Name,
            BaseUrl = p.Endpoint,
            Bucket = p.DefaultBucket,
            Region = p.Region,
            Priority = p.Priority,
            GeoRegion = geoRegion
        };
}
