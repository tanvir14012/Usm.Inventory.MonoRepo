using Usm.Shared.Algorithms.Collections.CircularBuffer.Abstractions;

namespace Usm.Shared.Algorithms.Collections.CircularBuffer.Builders;

/// <summary>
/// Fluent builder for circular buffer configuration.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class CircularBufferBuilder<T> : ICircularBufferBuilder<T>
{
    private int _capacity = 16;

    /// <inheritdoc />
    public ICircularBufferBuilder<T> WithCapacity(int capacity)
    {
        _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
        return this;
    }

    /// <inheritdoc />
    public ICircularBuffer<T> Build() => new CircularBuffer<T>(_capacity);
}
