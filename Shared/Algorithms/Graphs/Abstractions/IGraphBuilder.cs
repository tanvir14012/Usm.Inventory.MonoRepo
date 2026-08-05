using System.Numerics;

namespace Usm.Shared.Algorithms.Graphs.Abstractions;

/// <summary>
/// Builds graph instances.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public interface IGraphBuilder<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    /// <summary>Configures whether the graph is directed.</summary>
    IGraphBuilder<TVertex, TWeight> WithDirected(bool directed);

    /// <summary>Configures the vertex comparer.</summary>
    IGraphBuilder<TVertex, TWeight> WithComparer(IComparer<TVertex> comparer);

    /// <summary>Builds the graph.</summary>
    IGraph<TVertex, TWeight> Build();
}
