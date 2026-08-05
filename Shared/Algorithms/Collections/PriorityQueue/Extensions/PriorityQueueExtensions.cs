using Usm.Shared.Algorithms.Collections.PriorityQueue.Abstractions;
using Usm.Shared.Algorithms.Collections.PriorityQueue.Builders;

namespace Usm.Shared.Algorithms.Collections.PriorityQueue.Extensions;

/// <summary>
/// Common extension methods for priority queue creation.
/// </summary>
public static class PriorityQueueExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static IPriorityQueueBuilder<TItem, TPriority> CreateBuilder<TItem, TPriority>()
        where TItem : notnull
        where TPriority : notnull
        => new PriorityQueueBuilder<TItem, TPriority>();
}
