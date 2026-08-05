namespace Usm.Shared.Algorithms.Collections.IntervalTree;

/// <summary>
/// Represents a closed interval with a payload.
/// </summary>
/// <typeparam name="TKey">The boundary type.</typeparam>
/// <typeparam name="TValue">The payload type.</typeparam>
public readonly record struct Interval<TKey, TValue>(TKey Start, TKey End, TValue Value)
    where TKey : notnull;
