namespace Usm.Shared.Algorithms.Graphs;

/// <summary>
/// Represents a path through a graph.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public sealed record GraphPath<TVertex, TWeight>(bool Found, IReadOnlyList<TVertex> Vertices, TWeight Weight)
    where TVertex : notnull;
