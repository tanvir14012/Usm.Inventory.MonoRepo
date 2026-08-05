using Usm.Shared.Algorithms.Collections.CircularBuffer.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Collections.CircularBuffer.Tests;

public sealed class CircularBufferTests
{
    [Fact]
    public void OverwritesOldestItemWhenFull()
    {
        var buffer = CircularBufferExtensions.CreateBuilder<int>().WithCapacity(2).Build();
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);

        Assert.Equal(new[] { 2, 3 }, buffer.Snapshot());
    }

    [Fact]
    public void DequeuesInQueueOrder()
    {
        var buffer = CircularBufferExtensions.CreateBuilder<string>().WithCapacity(3).Build();
        buffer.Enqueue("a");
        buffer.Enqueue("b");

        Assert.Equal("a", buffer.Dequeue());
        Assert.Equal("b", buffer.Dequeue());
    }

    [Fact]
    public void SupportsTryPeekAndTryDequeue()
    {
        var buffer = CircularBufferExtensions.CreateBuilder<int>().WithCapacity(1).Build();
        buffer.Enqueue(7);

        Assert.True(buffer.TryPeek(out var peeked));
        Assert.Equal(7, peeked);

        Assert.True(buffer.TryDequeue(out var dequeued));
        Assert.Equal(7, dequeued);
        Assert.False(buffer.TryDequeue(out _));
    }
}
