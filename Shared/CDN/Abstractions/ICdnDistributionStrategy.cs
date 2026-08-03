using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Strategy pattern interface for CDN distribution.  Each implementation encapsulates one routing/processing
/// heuristic.  The <see cref="CdnDistributionOrchestrator"/> selects the highest-priority applicable strategy.
/// </summary>
public interface ICdnDistributionStrategy
{
    /// <summary>Human-readable strategy identifier used in telemetry and logging.</summary>
    string Name { get; }

    /// <summary>Evaluation order – lower values are tried first.</summary>
    int Priority { get; }

    /// <summary>Returns true when this strategy is applicable for the given <paramref name="context"/>.</summary>
    bool CanHandle(DistributionContext context);

    /// <summary>Executes the strategy and returns a routing/processing result.</summary>
    ValueTask<DistributionResult> ExecuteAsync(DistributionContext context, CancellationToken cancellationToken = default);
}
