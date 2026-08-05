using Usm.Shared.Algorithms.Distributed.Abstractions;

namespace Usm.Shared.Algorithms.Distributed.Builders;

/// <summary>
/// Fluent builder for distributed algorithms.
/// </summary>
public sealed class DistributedAlgorithmsBuilder : IDistributedAlgorithmsBuilder
{
    /// <inheritdoc />
    public IDistributedAlgorithms Build() => new Implementation.DistributedAlgorithms();
}
