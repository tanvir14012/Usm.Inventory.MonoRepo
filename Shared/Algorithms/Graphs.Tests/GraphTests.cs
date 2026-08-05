using Usm.Shared.Algorithms.Graphs.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Graphs.Tests;

public sealed class GraphTests
{
    [Fact]
    public void TraversesBreadthFirstAndDepthFirst()
    {
        var graph = GraphExtensions.CreateBuilder<string, int>().WithDirected(true).Build();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");
        graph.AddEdge("C", "D");

        Assert.Equal(new[] { "A", "B", "C", "D" }, graph.BreadthFirstSearch("A"));
        Assert.Equal(4, graph.DepthFirstSearch("A").Count);
    }

    [Fact]
    public void SortsTopologicallyAndDetectsCycles()
    {
        var graph = GraphExtensions.CreateBuilder<string, int>().WithDirected(true).Build();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");

        Assert.Equal(new[] { "A", "B", "C" }, graph.TopologicalSort());
        Assert.False(graph.HasCycle());

        graph.AddEdge("C", "A");
        Assert.True(graph.HasCycle());
    }

    [Fact]
    public void FindsShortestPaths()
    {
        var graph = GraphExtensions.CreateBuilder<string, int>().WithDirected(true).Build();
        graph.AddEdge("A", "B", 2);
        graph.AddEdge("A", "C", 5);
        graph.AddEdge("B", "C", 1);
        graph.AddEdge("C", "D", 1);

        var dijkstra = graph.Dijkstra("A", "D");
        Assert.True(dijkstra.Found);
        Assert.Equal(new[] { "A", "B", "C", "D" }, dijkstra.Vertices);
        Assert.Equal(4, dijkstra.Weight);

        var aStar = graph.AStar("A", "D", (_, _) => 0);
        Assert.True(aStar.Found);
        Assert.Equal(4, aStar.Weight);

        var bellmanFord = graph.BellmanFord("A");
        Assert.Equal(0, bellmanFord["A"]);
        Assert.Equal(2, bellmanFord["B"]);
        Assert.Equal(3, bellmanFord["C"]);
        Assert.Equal(4, bellmanFord["D"]);
    }

    [Fact]
    public async Task SupportsAsyncOperations()
    {
        var graph = GraphExtensions.CreateBuilder<string, int>().WithDirected(true).Build();
        graph.AddEdge("A", "B", 1);

        Assert.Equal(2, (await graph.BreadthFirstSearchAsync("A")).Count);
        Assert.False(await graph.HasCycleAsync());
    }

    [Fact]
    public void ClearsState()
    {
        var graph = GraphExtensions.CreateBuilder<string, int>().Build();
        graph.AddEdge("A", "B");
        graph.Clear();

        Assert.Equal(0, graph.VertexCount);
        Assert.Equal(0, graph.EdgeCount);
    }
}
