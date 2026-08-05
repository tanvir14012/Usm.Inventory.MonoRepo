using Usm.Shared.Algorithms.Compression.Abstractions;

namespace Usm.Shared.Algorithms.Compression.Builders;

/// <summary>
/// Fluent builder for compression algorithms.
/// </summary>
public sealed class CompressionAlgorithmsBuilder : ICompressionAlgorithmsBuilder
{
    /// <inheritdoc />
    public ICompressionAlgorithms Build() => new Implementation.CompressionAlgorithms();
}
