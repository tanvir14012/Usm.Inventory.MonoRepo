using System.Numerics;
using Usm.Shared.Algorithms.Graphs.Abstractions;

namespace Usm.Shared.Algorithms.Graphs.Builders;

/// <summary>
/// Fluent builder for graphs.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public sealed class GraphBuilder<TVertex, TWeight> : IGraphBuilder<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    private readonly GraphOptions<TVertex> _options = new();

    /// <inheritdoc />
    public IGraphBuilder<TVertex, TWeight> WithDirected(bool directed)
    {
        _options.Directed = directed;
        return this;
    }

    /// <inheritdoc />
    public IGraphBuilder<TVertex, TWeight> WithComparer(IComparer<TVertex> comparer)
    {
        _options.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    /// <inheritdoc />
    public IGraph<TVertex, TWeight> Build() => new Graph<TVertex, TWeight>(_options);
}
