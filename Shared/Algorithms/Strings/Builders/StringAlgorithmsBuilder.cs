using Usm.Shared.Algorithms.Strings.Abstractions;

namespace Usm.Shared.Algorithms.Strings.Builders;

/// <summary>
/// Fluent builder for string algorithms.
/// </summary>
public sealed class StringAlgorithmsBuilder : IStringAlgorithmsBuilder
{
    /// <inheritdoc />
    public IStringAlgorithms Build() => new Implementation.StringAlgorithms();
}
