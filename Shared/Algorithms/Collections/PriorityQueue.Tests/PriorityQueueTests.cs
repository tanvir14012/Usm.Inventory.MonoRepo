using Usm.Shared.Algorithms.Collections.PriorityQueue.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.PriorityQueue.Tests;

public sealed class PriorityQueueTests
{
    [Fact]
    public void DequeuesLowestPriorityFirst()
    {
        var queue = PriorityQueueExtensions.CreateBuilder<string, int>().Build();
        queue.Enqueue("low", 10);
        queue.Enqueue("high", 1);

        Assert.Equal("high", queue.Dequeue());
        Assert.Equal("low", queue.Dequeue());
    }

    [Fact]
    public void PreservesInsertionOrderForEqualPriorityWhenStable()
    {
        var queue = PriorityQueueExtensions.CreateBuilder<string, int>().WithStableOrdering(true).Build();
        queue.Enqueue("a", 1);
        queue.Enqueue("b", 1);

        Assert.Equal("a", queue.Dequeue());
        Assert.Equal("b", queue.Dequeue());
    }

    [Fact]
    public void SupportsTryPeekAndTryDequeue()
    {
        var queue = PriorityQueueExtensions.CreateBuilder<string, int>().Build();
        queue.Enqueue("a", 2);

        Assert.True(queue.TryPeek(out var item, out var priority));
        Assert.Equal("a", item);
        Assert.Equal(2, priority);

        Assert.True(queue.TryDequeue(out var dequeued, out var dequeuedPriority));
        Assert.Equal("a", dequeued);
        Assert.Equal(2, dequeuedPriority);
        Assert.False(queue.TryDequeue(out _, out _));
    }
}
