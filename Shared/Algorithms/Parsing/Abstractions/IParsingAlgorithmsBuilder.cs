namespace Usm.Shared.Algorithms.Parsing.Abstractions;

/// <summary>
/// Builds parsing algorithm instances.
/// </summary>
public interface IParsingAlgorithmsBuilder
{
    /// <summary>Builds the parsing algorithm set.</summary>
    IParsingAlgorithms Build();
}
