using System.Numerics;

namespace Usm.Shared.Algorithms.Graphs.Abstractions;

/// <summary>
/// Represents reusable graph algorithms.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public interface IGraphAlgorithms<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    /// <summary>Runs breadth-first search.</summary>
    IReadOnlyList<TVertex> BreadthFirstSearch(TVertex start);

    /// <summary>Runs depth-first search.</summary>
    IReadOnlyList<TVertex> DepthFirstSearch(TVertex start);

    /// <summary>Performs a topological sort.</summary>
    IReadOnlyList<TVertex> TopologicalSort();

    /// <summary>Detects whether the graph contains a cycle.</summary>
    bool HasCycle();

    /// <summary>Computes the shortest path using Dijkstra's algorithm.</summary>
    GraphPath<TVertex, TWeight> Dijkstra(TVertex start, TVertex goal);

    /// <summary>Computes the shortest path using A* search.</summary>
    GraphPath<TVertex, TWeight> AStar(TVertex start, TVertex goal, Func<TVertex, TVertex, TWeight> heuristic);

    /// <summary>Computes shortest paths using Bellman-Ford.</summary>
    IReadOnlyDictionary<TVertex, TWeight> BellmanFord(TVertex start);

    /// <summary>Runs breadth-first search asynchronously.</summary>
    ValueTask<IReadOnlyList<TVertex>> BreadthFirstSearchAsync(TVertex start, CancellationToken cancellationToken = default);

    /// <summary>Runs depth-first search asynchronously.</summary>
    ValueTask<IReadOnlyList<TVertex>> DepthFirstSearchAsync(TVertex start, CancellationToken cancellationToken = default);

    /// <summary>Performs a topological sort asynchronously.</summary>
    ValueTask<IReadOnlyList<TVertex>> TopologicalSortAsync(CancellationToken cancellationToken = default);

    /// <summary>Detects cycles asynchronously.</summary>
    ValueTask<bool> HasCycleAsync(CancellationToken cancellationToken = default);

    /// <summary>Computes a shortest path asynchronously.</summary>
    ValueTask<GraphPath<TVertex, TWeight>> DijkstraAsync(TVertex start, TVertex goal, CancellationToken cancellationToken = default);

    /// <summary>Computes a shortest path asynchronously with A*.</summary>
    ValueTask<GraphPath<TVertex, TWeight>> AStarAsync(TVertex start, TVertex goal, Func<TVertex, TVertex, TWeight> heuristic, CancellationToken cancellationToken = default);

    /// <summary>Computes Bellman-Ford distances asynchronously.</summary>
    ValueTask<IReadOnlyDictionary<TVertex, TWeight>> BellmanFordAsync(TVertex start, CancellationToken cancellationToken = default);
}
