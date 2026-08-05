using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Sidecar.Abstractions;
using Usm.Shared.Patterns.Sidecar.Extensions;

namespace Usm.Shared.Patterns.Sidecar.Builders;

/// <summary>
/// Default fluent builder for <see cref="ISidecar{TService}"/>.
/// </summary>
/// <typeparam name="TService">The primary service contract.</typeparam>
public sealed class SidecarBuilder<TService> : ISidecarBuilder<TService> where TService : class
{
    private readonly SidecarOptions _options = new();

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithMaxAttempts(int maxAttempts)
    {
        _options.MaxAttempts = maxAttempts > 0
            ? maxAttempts
            : throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be at least 1.");
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithRetryBaseDelay(TimeSpan baseDelay)
    {
        _options.RetryBaseDelay = baseDelay >= TimeSpan.Zero
            ? baseDelay
            : throw new ArgumentOutOfRangeException(nameof(baseDelay));
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithRetryMaxDelay(TimeSpan maxDelay)
    {
        _options.RetryMaxDelay = maxDelay > TimeSpan.Zero
            ? maxDelay
            : throw new ArgumentOutOfRangeException(nameof(maxDelay));
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithRetryStrategy(SidecarRetryStrategy strategy)
    {
        _options.RetryStrategy = strategy;
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithJitter(bool enabled)
    {
        _options.UseJitter = enabled;
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithFailureThreshold(int threshold)
    {
        _options.FailureThreshold = threshold > 0
            ? threshold
            : throw new ArgumentOutOfRangeException(nameof(threshold), "Must be at least 1.");
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithCircuitOpenDuration(TimeSpan duration)
    {
        _options.CircuitOpenDuration = duration > TimeSpan.Zero
            ? duration
            : throw new ArgumentOutOfRangeException(nameof(duration));
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithHalfOpenPermits(int permits)
    {
        _options.HalfOpenPermits = permits > 0
            ? permits
            : throw new ArgumentOutOfRangeException(nameof(permits), "Must be at least 1.");
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithExecutionTimeout(TimeSpan? timeout)
    {
        if (timeout is { } t && t <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        _options.ExecutionTimeout = timeout;
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithTimeProvider(TimeProvider timeProvider)
    {
        _options.TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }

    /// <inheritdoc />
    public ISidecarBuilder<TService> WithHealthCheckName(string name)
    {
        _options.HealthCheckName = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Health check name must not be blank.", nameof(name))
            : name;
        return this;
    }

    /// <inheritdoc />
    public ISidecar<TService> Build(TService primary)
        => new Sidecar<TService>(primary, Options.Create(_options));
}
