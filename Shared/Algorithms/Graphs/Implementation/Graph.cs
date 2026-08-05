using System.Numerics;
using Usm.Shared.Algorithms.Graphs.Abstractions;

namespace Usm.Shared.Algorithms.Graphs;

/// <summary>
/// Thread-safe adjacency-list graph with traversal and shortest-path algorithms.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The weight type.</typeparam>
public sealed class Graph<TVertex, TWeight> : IGraph<TVertex, TWeight>, IGraphAlgorithms<TVertex, TWeight>
    where TVertex : notnull
    where TWeight : INumber<TWeight>
{
    private readonly Dictionary<TVertex, List<GraphEdge<TVertex, TWeight>>> _adjacency;
    private readonly IEqualityComparer<TVertex> _vertexEqualityComparer;
    private readonly object _gate = new();

    /// <summary>Initializes a new graph.</summary>
    public Graph(GraphOptions<TVertex>? options = null)
    {
        Directed = options?.Directed ?? true;
        Comparer = options?.Comparer ?? Comparer<TVertex>.Default;
        _vertexEqualityComparer = new VertexEqualityComparer(Comparer);
        _adjacency = new Dictionary<TVertex, List<GraphEdge<TVertex, TWeight>>>(_vertexEqualityComparer);
    }

    /// <inheritdoc />
    public bool Directed { get; }

    /// <inheritdoc />
    public IComparer<TVertex> Comparer { get; }

    /// <inheritdoc />
    public int VertexCount
    {
        get
        {
            lock (_gate)
                return _adjacency.Count;
        }
    }

    /// <inheritdoc />
    public int EdgeCount { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<TVertex> Vertices
    {
        get
        {
            lock (_gate)
                return _adjacency.Keys.ToArray();
        }
    }

    /// <inheritdoc />
    public bool AddVertex(TVertex vertex)
    {
        lock (_gate)
        {
            if (_adjacency.ContainsKey(vertex))
                return false;

            _adjacency[vertex] = [];
            return true;
        }
    }

    /// <inheritdoc />
    public void AddEdge(TVertex from, TVertex to, TWeight weight)
    {
        lock (_gate)
        {
            AddVertexInternal(from);
            AddVertexInternal(to);

            _adjacency[from].Add(new GraphEdge<TVertex, TWeight>(from, to, weight));
            if (!Directed)
                _adjacency[to].Add(new GraphEdge<TVertex, TWeight>(to, from, weight));

            EdgeCount++;
        }
    }

    /// <inheritdoc />
    public void AddEdge(TVertex from, TVertex to) => AddEdge(from, to, TWeight.One);

    /// <inheritdoc />
    public bool ContainsVertex(TVertex vertex)
    {
        lock (_gate)
            return _adjacency.ContainsKey(vertex);
    }

    /// <inheritdoc />
    public bool ContainsEdge(TVertex from, TVertex to)
    {
        lock (_gate)
        {
            return _adjacency.TryGetValue(from, out var edges) && edges.Any(edge => Comparer.Compare(edge.To, to) == 0);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<GraphEdge<TVertex, TWeight>> OutgoingEdges(TVertex vertex)
    {
        lock (_gate)
        {
            return _adjacency.TryGetValue(vertex, out var edges) ? edges.ToArray() : [];
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _adjacency.Clear();
            EdgeCount = 0;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<TVertex> BreadthFirstSearch(TVertex start)
    {
        lock (_gate)
            return TraverseBreadthFirst(start);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<TVertex>> BreadthFirstSearchAsync(TVertex start, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(BreadthFirstSearch(start));
    }

    /// <inheritdoc />
    public IReadOnlyList<TVertex> DepthFirstSearch(TVertex start)
    {
        lock (_gate)
            return TraverseDepthFirst(start);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<TVertex>> DepthFirstSearchAsync(TVertex start, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(DepthFirstSearch(start));
    }

    /// <inheritdoc />
    public IReadOnlyList<TVertex> TopologicalSort()
    {
        lock (_gate)
            return TopologicalSortCore();
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<TVertex>> TopologicalSortAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(TopologicalSort());
    }

    /// <inheritdoc />
    public bool HasCycle()
    {
        lock (_gate)
            return Directed ? HasCycleDirected() : HasCycleUndirected();
    }

    /// <inheritdoc />
    public ValueTask<bool> HasCycleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(HasCycle());
    }

    /// <inheritdoc />
    public GraphPath<TVertex, TWeight> Dijkstra(TVertex start, TVertex goal)
    {
        lock (_gate)
            return DijkstraCore(start, goal);
    }

    /// <inheritdoc />
    public ValueTask<GraphPath<TVertex, TWeight>> DijkstraAsync(TVertex start, TVertex goal, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Dijkstra(start, goal));
    }

    /// <inheritdoc />
    public GraphPath<TVertex, TWeight> AStar(TVertex start, TVertex goal, Func<TVertex, TVertex, TWeight> heuristic)
    {
        ArgumentNullException.ThrowIfNull(heuristic);

        lock (_gate)
            return AStarCore(start, goal, heuristic);
    }

    /// <inheritdoc />
    public ValueTask<GraphPath<TVertex, TWeight>> AStarAsync(TVertex start, TVertex goal, Func<TVertex, TVertex, TWeight> heuristic, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AStar(start, goal, heuristic));
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<TVertex, TWeight> BellmanFord(TVertex start)
    {
        lock (_gate)
            return BellmanFordCore(start);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyDictionary<TVertex, TWeight>> BellmanFordAsync(TVertex start, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(BellmanFord(start));
    }

    private void AddVertexInternal(TVertex vertex)
    {
        if (!_adjacency.ContainsKey(vertex))
            _adjacency[vertex] = [];
    }

    private IReadOnlyList<TVertex> TraverseBreadthFirst(TVertex start)
    {
        if (!_adjacency.ContainsKey(start))
            return [];

        var visited = new HashSet<TVertex>(_vertexEqualityComparer) { start };
        var queue = new Queue<TVertex>();
        var order = new List<TVertex>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var vertex = queue.Dequeue();
            order.Add(vertex);
            foreach (var edge in _adjacency[vertex])
            {
                if (visited.Add(edge.To))
                    queue.Enqueue(edge.To);
            }
        }

        return order;
    }

    private IReadOnlyList<TVertex> TraverseDepthFirst(TVertex start)
    {
        if (!_adjacency.ContainsKey(start))
            return [];

        var visited = new HashSet<TVertex>(_vertexEqualityComparer);
        var stack = new Stack<TVertex>();
        var order = new List<TVertex>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var vertex = stack.Pop();
            if (!visited.Add(vertex))
                continue;

            order.Add(vertex);
            var edges = _adjacency[vertex];
            for (var i = edges.Count - 1; i >= 0; i--)
                stack.Push(edges[i].To);
        }

        return order;
    }

    private IReadOnlyList<TVertex> TopologicalSortCore()
    {
        if (!Directed)
            throw new InvalidOperationException("Topological sort requires a directed graph.");

        var indegree = _adjacency.Keys.ToDictionary(vertex => vertex, _ => 0, _vertexEqualityComparer);
        foreach (var edges in _adjacency.Values)
        {
            foreach (var edge in edges)
                indegree[edge.To]++;
        }

        var queue = new Queue<TVertex>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var order = new List<TVertex>();

        while (queue.Count > 0)
        {
            var vertex = queue.Dequeue();
            order.Add(vertex);
            foreach (var edge in _adjacency[vertex])
            {
                indegree[edge.To]--;
                if (indegree[edge.To] == 0)
                    queue.Enqueue(edge.To);
            }
        }

        if (order.Count != _adjacency.Count)
            throw new InvalidOperationException("The graph contains a cycle.");

        return order;
    }

    private bool HasCycleDirected()
    {
        var state = _adjacency.Keys.ToDictionary(vertex => vertex, _ => 0, _vertexEqualityComparer);

        foreach (var vertex in _adjacency.Keys)
        {
            if (state[vertex] == 0 && HasCycleDirected(vertex, state))
                return true;
        }

        return false;
    }

    private bool HasCycleDirected(TVertex vertex, Dictionary<TVertex, int> state)
    {
        state[vertex] = 1;
        foreach (var edge in _adjacency[vertex])
        {
            if (state[edge.To] == 1)
                return true;

            if (state[edge.To] == 0 && HasCycleDirected(edge.To, state))
                return true;
        }

        state[vertex] = 2;
        return false;
    }

    private bool HasCycleUndirected()
    {
        var visited = new HashSet<TVertex>(_vertexEqualityComparer);
        foreach (var vertex in _adjacency.Keys)
        {
            if (!visited.Contains(vertex) && HasCycleUndirected(vertex, default, visited))
                return true;
        }

        return false;
    }

    private bool HasCycleUndirected(TVertex vertex, TVertex? parent, HashSet<TVertex> visited)
    {
        visited.Add(vertex);
        foreach (var edge in _adjacency[vertex])
        {
            if (parent is not null && Comparer.Compare(edge.To, parent) == 0)
                continue;

            if (visited.Contains(edge.To) || HasCycleUndirected(edge.To, vertex, visited))
                return true;
        }

        return false;
    }

    private GraphPath<TVertex, TWeight> DijkstraCore(TVertex start, TVertex goal)
    {
        if (!_adjacency.ContainsKey(start) || !_adjacency.ContainsKey(goal))
            return new GraphPath<TVertex, TWeight>(false, [], default!);

        var distances = new Dictionary<TVertex, TWeight>(_vertexEqualityComparer) { [start] = TWeight.Zero };
        var previous = new Dictionary<TVertex, TVertex>(_vertexEqualityComparer);
        var queue = new PriorityQueue<TVertex, TWeight>();
        queue.Enqueue(start, TWeight.Zero);

        while (queue.TryDequeue(out var vertex, out var priority))
        {
            if (distances.TryGetValue(vertex, out var current) && priority > current)
                continue;

            if (!distances.TryGetValue(vertex, out current))
                current = priority;

            if (Comparer.Compare(vertex, goal) == 0)
                break;

            foreach (var edge in _adjacency[vertex])
            {
                var candidate = current + edge.Weight;
                if (!distances.TryGetValue(edge.To, out var existing) || candidate < existing)
                {
                    distances[edge.To] = candidate;
                    previous[edge.To] = vertex;
                    queue.Enqueue(edge.To, candidate);
                }
            }
        }

        if (!distances.TryGetValue(goal, out var distance))
            return new GraphPath<TVertex, TWeight>(false, [], default!);

        return new GraphPath<TVertex, TWeight>(true, ReconstructPath(previous, start, goal), distance);
    }

    private GraphPath<TVertex, TWeight> AStarCore(TVertex start, TVertex goal, Func<TVertex, TVertex, TWeight> heuristic)
    {
        if (!_adjacency.ContainsKey(start) || !_adjacency.ContainsKey(goal))
            return new GraphPath<TVertex, TWeight>(false, [], default!);

        var gScore = new Dictionary<TVertex, TWeight>(_vertexEqualityComparer) { [start] = TWeight.Zero };
        var previous = new Dictionary<TVertex, TVertex>(_vertexEqualityComparer);
        var open = new PriorityQueue<TVertex, TWeight>();
        open.Enqueue(start, heuristic(start, goal));

        while (open.TryDequeue(out var current, out _))
        {
            if (Comparer.Compare(current, goal) == 0)
                return new GraphPath<TVertex, TWeight>(true, ReconstructPath(previous, start, goal), gScore[current]);

            foreach (var edge in _adjacency[current])
            {
                var tentative = gScore[current] + edge.Weight;
                if (!gScore.TryGetValue(edge.To, out var existing) || tentative < existing)
                {
                    gScore[edge.To] = tentative;
                    previous[edge.To] = current;
                    open.Enqueue(edge.To, tentative + heuristic(edge.To, goal));
                }
            }
        }

        return new GraphPath<TVertex, TWeight>(false, [], default!);
    }

    private IReadOnlyDictionary<TVertex, TWeight> BellmanFordCore(TVertex start)
    {
        if (!_adjacency.ContainsKey(start))
            return new Dictionary<TVertex, TWeight>(_vertexEqualityComparer);

        var distances = new Dictionary<TVertex, TWeight>(_vertexEqualityComparer) { [start] = TWeight.Zero };
        var vertices = _adjacency.Keys.ToArray();
        var edges = _adjacency.Values.SelectMany(list => list).ToArray();

        for (var i = 0; i < vertices.Length - 1; i++)
        {
            var updated = false;
            foreach (var edge in edges)
            {
                if (!distances.TryGetValue(edge.From, out var fromDistance))
                    continue;

                var candidate = fromDistance + edge.Weight;
                if (!distances.TryGetValue(edge.To, out var existing) || candidate < existing)
                {
                    distances[edge.To] = candidate;
                    updated = true;
                }
            }

            if (!updated)
                break;
        }

        foreach (var edge in edges)
        {
            if (!distances.TryGetValue(edge.From, out var fromDistance))
                continue;

            var candidate = fromDistance + edge.Weight;
            if (distances.TryGetValue(edge.To, out var existing) && candidate < existing)
                throw new InvalidOperationException("The graph contains a negative-weight cycle.");
        }

        return distances;
    }

    private IReadOnlyList<TVertex> ReconstructPath(Dictionary<TVertex, TVertex> previous, TVertex start, TVertex goal)
    {
        var path = new List<TVertex>();
        var current = goal;
        path.Add(current);

        while (Comparer.Compare(current, start) != 0)
        {
            if (!previous.TryGetValue(current, out var parent))
                return [];

            current = parent;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private sealed class VertexEqualityComparer : IEqualityComparer<TVertex>
    {
        private readonly IComparer<TVertex> _comparer;

        public VertexEqualityComparer(IComparer<TVertex> comparer)
        {
            _comparer = comparer;
        }

        public bool Equals(TVertex? x, TVertex? y) => x is not null && y is not null && _comparer.Compare(x, y) == 0;

        public int GetHashCode(TVertex obj) => EqualityComparer<TVertex>.Default.GetHashCode(obj);
    }
}
