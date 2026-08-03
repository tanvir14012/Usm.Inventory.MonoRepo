using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Strategies;

/// <summary>
/// Distributes load across multiple redundant storage providers using weighted round-robin.
///
/// Weight formula: each provider receives 10 − Priority slots in the rotation table,
/// so a Priority=0 provider gets 10× the traffic of a Priority=9 provider.
/// Minimum slot count is 1 (clamped from below).
///
/// Priority: 20 (fallback when more specific strategies decline).
/// Activates when: more than one storage provider is configured.
/// </summary>
internal sealed class LoadDistributionStrategy(
    IStorageProviderEngine storageEngine,
    IOptions<CdnOptions> options,
    ILogger<LoadDistributionStrategy> logger) : ICdnDistributionStrategy
{
    private readonly CdnOptions _opts = options.Value;

    // Interlocked counter drives round-robin; overflow wraps safely for int
    private int _counter;

    public string Name => "LoadDistribution";
    public int Priority => 20;

    public bool CanHandle(DistributionContext context)
        => _opts.StorageProviders.Length > 1;

    public async ValueTask<DistributionResult> ExecuteAsync(
        DistributionContext context, CancellationToken cancellationToken = default)
    {
        var selected = PickProvider();

        logger.LogDebug("[{Strategy}] Selected '{Provider}' via weighted round-robin", Name, selected.Name);

        var metadata = await storageEngine
            .GetMetadataAsync(context.Bucket, context.AssetKey, cancellationToken)
            .ConfigureAwait(false);

        return new DistributionResult
        {
            StrategyUsed = Name,
            Endpoint = new StorageEndpoint
            {
                ProviderName = selected.Name,
                BaseUrl = selected.Endpoint,
                Bucket = selected.DefaultBucket,
                Region = selected.Region,
                Priority = selected.Priority
            },
            ResolvedMetadata = metadata
        };
    }

    private StorageProviderOptions PickProvider()
    {
        var providers = _opts.StorageProviders.OrderBy(p => p.Priority).ToArray();
        if (providers.Length == 0)
            throw new InvalidOperationException("No storage providers configured.");

        // Build weighted rotation table
        var table = providers
            .SelectMany(p => Enumerable.Repeat(p, Math.Max(1, 10 - p.Priority)))
            .ToArray();

        var idx = Math.Abs(Interlocked.Increment(ref _counter) % table.Length);
        return table[idx];
    }
}
