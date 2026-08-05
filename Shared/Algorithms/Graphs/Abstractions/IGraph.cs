using System.Collections.Generic;
using System.Numerics;

namespace Usm.Shared.Algorithms.Graphs.Abstractions;

/// <summary>
/// Represents a reusable graph.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public interface IGraph<TVertex, TWeight>
    : IGraphAlgorithms<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    /// <summary>Gets whether the graph is directed.</summary>
    bool Directed { get; }

    /// <summary>Gets the comparer used to order vertices.</summary>
    IComparer<TVertex> Comparer { get; }

    /// <summary>Gets the number of vertices.</summary>
    int VertexCount { get; }

    /// <summary>Gets the number of edges.</summary>
    int EdgeCount { get; }

    /// <summary>Adds a vertex.</summary>
    bool AddVertex(TVertex vertex);

    /// <summary>Adds an edge with explicit weight.</summary>
    void AddEdge(TVertex from, TVertex to, TWeight weight);

    /// <summary>Adds an unweighted edge.</summary>
    void AddEdge(TVertex from, TVertex to);

    /// <summary>Determines whether a vertex exists.</summary>
    bool ContainsVertex(TVertex vertex);

    /// <summary>Determines whether an edge exists.</summary>
    bool ContainsEdge(TVertex from, TVertex to);

    /// <summary>Gets the outgoing edges for a vertex.</summary>
    IReadOnlyList<GraphEdge<TVertex, TWeight>> OutgoingEdges(TVertex vertex);

    /// <summary>Returns all vertices.</summary>
    IReadOnlyCollection<TVertex> Vertices { get; }

    /// <summary>Clears the graph.</summary>
    void Clear();
}
