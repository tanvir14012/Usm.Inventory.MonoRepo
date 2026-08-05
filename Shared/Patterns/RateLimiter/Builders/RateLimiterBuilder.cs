using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usm.Shared.Patterns.RateLimiter.Abstractions;

namespace Usm.Shared.Patterns.RateLimiter.Builders;

/// <summary>
/// Fluent builder for a reusable rate limiter.
/// </summary>
/// <typeparam name="TContext">The calling context type.</typeparam>
public sealed class RateLimiterBuilder<TContext> : IRateLimiterBuilder<TContext>
{
    private readonly RateLimiterOptions _options = new();
    private ILogger<IRateLimiter<TContext>>? _logger;

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithAlgorithm(RateLimiterAlgorithm algorithm)
    {
        _options.Algorithm = algorithm;
        return this;
    }

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithPermitLimit(int permitLimit)
    {
        _options.PermitLimit = permitLimit > 0 ? permitLimit : throw new ArgumentOutOfRangeException(nameof(permitLimit));
        return this;
    }

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithWindow(TimeSpan window)
    {
        _options.Window = window > TimeSpan.Zero ? window : throw new ArgumentOutOfRangeException(nameof(window));
        return this;
    }

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithSegments(int segments)
    {
        _options.Segments = segments > 0 ? segments : throw new ArgumentOutOfRangeException(nameof(segments));
        return this;
    }

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithQueueLimit(int queueLimit)
    {
        _options.QueueLimit = queueLimit > 0 ? queueLimit : throw new ArgumentOutOfRangeException(nameof(queueLimit));
        return this;
    }

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithTimeProvider(TimeProvider timeProvider)
    {
        _options.TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        return this;
    }

    /// <inheritdoc />
    public IRateLimiterBuilder<TContext> WithLogger(ILogger<IRateLimiter<TContext>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <inheritdoc />
    public IRateLimiter<TContext> Build()
        => new RateLimiter<TContext>(_options, _logger ?? NullLogger<IRateLimiter<TContext>>.Instance);
}
