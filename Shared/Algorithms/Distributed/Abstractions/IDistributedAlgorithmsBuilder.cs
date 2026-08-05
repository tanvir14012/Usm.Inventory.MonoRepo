namespace Usm.Shared.Algorithms.Distributed.Abstractions;

/// <summary>
/// Builds distributed algorithm instances.
/// </summary>
public interface IDistributedAlgorithmsBuilder
{
    /// <summary>Builds the distributed algorithm set.</summary>
    IDistributedAlgorithms Build();
}
