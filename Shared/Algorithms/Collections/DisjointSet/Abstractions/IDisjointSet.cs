namespace Usm.Shared.Algorithms.Collections.DisjointSet.Abstractions;

/// <summary>
/// Represents a disjoint-set / union-find data structure.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IDisjointSet<T>
    where T : notnull
{
    /// <summary>Gets the number of tracked elements.</summary>
    int Count { get; }

    /// <summary>Adds an element if it is not already present.</summary>
    bool Add(T item);

    /// <summary>Finds the canonical representative for the item.</summary>
    T Find(T item);

    /// <summary>Unions two sets and returns the canonical representative.</summary>
    T Union(T first, T second);

    /// <summary>Determines whether two items are connected.</summary>
    bool Connected(T first, T second);

    /// <summary>Returns the size of the set containing the item.</summary>
    int SetSize(T item);
}
