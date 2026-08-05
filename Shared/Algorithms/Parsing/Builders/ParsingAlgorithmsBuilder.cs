using Usm.Shared.Algorithms.Parsing.Abstractions;

namespace Usm.Shared.Algorithms.Parsing.Builders;

/// <summary>
/// Fluent builder for parsing algorithms.
/// </summary>
public sealed class ParsingAlgorithmsBuilder : IParsingAlgorithmsBuilder
{
    /// <inheritdoc />
    public IParsingAlgorithms Build() => new Implementation.ParsingAlgorithms();
}
