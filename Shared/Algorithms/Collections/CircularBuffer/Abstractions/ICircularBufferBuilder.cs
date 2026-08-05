namespace Usm.Shared.Algorithms.Collections.CircularBuffer.Abstractions;

/// <summary>
/// Fluent builder for circular buffer configuration.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public interface ICircularBufferBuilder<T>
{
    /// <summary>Sets the capacity.</summary>
    ICircularBufferBuilder<T> WithCapacity(int capacity);

    /// <summary>Builds the buffer.</summary>
    ICircularBuffer<T> Build();
}
