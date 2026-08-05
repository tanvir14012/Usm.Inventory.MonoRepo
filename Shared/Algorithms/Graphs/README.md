# Graphs

Reusable graph storage plus traversal and shortest-path algorithms.

## Example

```csharp
var graph = GraphExtensions.CreateBuilder<string, int>().WithDirected(true).Build();
graph.AddEdge("A", "B", 3);
graph.AddEdge("B", "C", 2);
var path = graph.Dijkstra("A", "C");
```

## Complexity

- BFS/DFS: `O(V + E)`
- Topological sort: `O(V + E)`
- Dijkstra: `O((V + E) log V)`
- Bellman-Ford: `O(VE)`
- A*: `O((V + E) log V)` worst case
