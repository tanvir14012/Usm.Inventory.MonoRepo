namespace Usm.Shared.Algorithms.Compression.Abstractions;

/// <summary>
/// Builds compression algorithm instances.
/// </summary>
public interface ICompressionAlgorithmsBuilder
{
    /// <summary>Builds the compression algorithm set.</summary>
    ICompressionAlgorithms Build();
}
