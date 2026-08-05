namespace Usm.Shared.Algorithms.Graphs;

/// <summary>
/// Configuration for graph instances.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
public sealed class GraphOptions<TVertex>
    where TVertex : notnull
{
    /// <summary>Gets or sets whether the graph is directed.</summary>
    public bool Directed { get; set; } = true;

    /// <summary>Gets or sets the vertex comparer.</summary>
    public IComparer<TVertex> Comparer { get; set; } = Comparer<TVertex>.Default;
}
