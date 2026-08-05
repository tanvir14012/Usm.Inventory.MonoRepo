namespace Usm.Shared.Algorithms.Strings.Abstractions;

/// <summary>
/// Builds string algorithm instances.
/// </summary>
public interface IStringAlgorithmsBuilder
{
    /// <summary>Builds the string algorithm set.</summary>
    IStringAlgorithms Build();
}
