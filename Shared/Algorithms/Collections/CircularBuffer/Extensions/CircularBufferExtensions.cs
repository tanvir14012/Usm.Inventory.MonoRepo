using Usm.Shared.Algorithms.Collections.CircularBuffer.Abstractions;
using Usm.Shared.Algorithms.Collections.CircularBuffer.Builders;

namespace Usm.Shared.Algorithms.Collections.CircularBuffer.Extensions;

/// <summary>
/// Common extension methods for circular buffer creation.
/// </summary>
public static class CircularBufferExtensions
{
    /// <summary>Creates a new builder.</summary>
    public static ICircularBufferBuilder<T> CreateBuilder<T>() => new CircularBufferBuilder<T>();
}
