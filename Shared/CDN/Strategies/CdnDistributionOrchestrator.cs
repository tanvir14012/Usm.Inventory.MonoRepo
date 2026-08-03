using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

namespace Usm.Shared.Infrastructure.CDN.Strategies;

/// <summary>
/// Evaluates all registered <see cref="ICdnDistributionStrategy"/> implementations in
/// ascending <see cref="ICdnDistributionStrategy.Priority"/> order and executes the first
/// one whose <see cref="ICdnDistributionStrategy.CanHandle"/> returns true.
///
/// If a strategy throws a non-cancellation exception it is logged and the next eligible
/// strategy is tried (graceful degradation).  An <see cref="InvalidOperationException"/>
/// is thrown only when no strategy can produce a result.
///
/// Default priority order:
///   1  EdgeProcessing  (image transformation intercept)
///   5  OriginShield    (Redis metadata cache / thundering-herd guard)
///   10 RegionalSharding (geo-aware / consistent-hash routing)
///   20 LoadDistribution (weighted round-robin fallback)
/// </summary>
public sealed class CdnDistributionOrchestrator(
    IEnumerable<ICdnDistributionStrategy> strategies,
    IOptions<CdnOptions> options,
    ILogger<CdnDistributionOrchestrator> logger)
{
    private readonly IReadOnlyList<ICdnDistributionStrategy> _strategies =
        [.. strategies.OrderBy(s => s.Priority)];

    private readonly CdnOptions _opts = options.Value;

    /// <summary>
    /// Selects and executes the best distribution strategy for the given context.
    /// </summary>
    public async ValueTask<DistributionResult> DistributeAsync(
        DistributionContext context, CancellationToken cancellationToken = default)
    {
        foreach (var strategy in _strategies)
        {
            if (!strategy.CanHandle(context))
                continue;

            try
            {
                var result = await strategy
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "[CDN] {Key} → strategy={Strategy} provider={Provider} cached={Cached}",
                    context.AssetKey, strategy.Name,
                    result.Endpoint.ProviderName, result.ServedFromCache);

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[CDN] Strategy '{Strategy}' failed for '{Key}' – trying next",
                    strategy.Name, context.AssetKey);
            }
        }

        throw new InvalidOperationException(
            $"No CDN distribution strategy could handle asset '{context.AssetKey}' in bucket '{context.Bucket}'.");
    }

    /// <summary>
    /// Convenience factory: builds a <see cref="DistributionContext"/> from the most common
    /// HTTP request components without requiring callers to reference the model directly.
    /// </summary>
    public static DistributionContext BuildContext(
        string bucket,
        string assetKey,
        string? clientIp = null,
        string? clientRegion = null,
        string? acceptHeader = null,
        string? rangeHeader = null,
        string? ifNoneMatch = null,
        IDictionary<string, string>? queryParams = null,
        MediaProcessingRequest? processingRequest = null)
        => new()
        {
            Bucket = bucket,
            AssetKey = assetKey,
            ClientIp = clientIp,
            ClientRegion = clientRegion,
            AcceptHeader = acceptHeader,
            RangeHeader = rangeHeader,
            IfNoneMatch = ifNoneMatch,
            QueryParams = queryParams ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            ProcessingRequest = processingRequest
        };
}
