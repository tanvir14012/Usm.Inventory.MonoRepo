namespace Usm.Shared.Algorithms.Graphs;

/// <summary>
/// Represents a graph edge.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public readonly record struct GraphEdge<TVertex, TWeight>(TVertex From, TVertex To, TWeight Weight)
    where TVertex : notnull;
