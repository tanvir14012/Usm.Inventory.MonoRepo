using Microsoft.Extensions.Logging;
using Usm.Shared.Patterns.RateLimiter.Abstractions;

namespace Usm.Shared.Patterns.RateLimiter;

internal sealed class RateLimiter<TContext> : IRateLimiter<TContext>
{
    private readonly RateLimiterOptions _options;
    private readonly ILogger<IRateLimiter<TContext>> _logger;
    private readonly object _gate = new();

    private double _tokenBucketTokens;
    private DateTimeOffset _tokenBucketRefillAt;

    private DateTimeOffset _fixedWindowStart;
    private int _fixedWindowCount;

    private DateTimeOffset _slidingWindowStart;
    private readonly int[] _slidingWindowSegments;
    private int _slidingWindowCurrentSegment;

    private double _leakyBucketLevel;
    private DateTimeOffset _leakyBucketDrainAt;

    public RateLimiter(RateLimiterOptions options, ILogger<IRateLimiter<TContext>> logger)
    {
        _options = options;
        _logger = logger;
        _tokenBucketTokens = options.PermitLimit;
        _tokenBucketRefillAt = options.TimeProvider.GetUtcNow();
        _fixedWindowStart = options.TimeProvider.GetUtcNow();
        _slidingWindowStart = options.TimeProvider.GetUtcNow();
        _slidingWindowSegments = new int[options.Segments];
        _leakyBucketDrainAt = options.TimeProvider.GetUtcNow();
    }

    public ValueTask<RateLimitLease> AcquireAsync(TContext context, int permits = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (permits <= 0)
            throw new ArgumentOutOfRangeException(nameof(permits));

        var lease = Acquire(context, permits, _options.TimeProvider.GetUtcNow());
        return ValueTask.FromResult(lease);
    }

    private RateLimitLease Acquire(TContext context, int permits, DateTimeOffset utcNow)
    {
        lock (_gate)
        {
            var lease = _options.Algorithm switch
            {
                RateLimiterAlgorithm.TokenBucket => AcquireTokenBucket(permits, utcNow),
                RateLimiterAlgorithm.FixedWindow => AcquireFixedWindow(permits, utcNow),
                RateLimiterAlgorithm.SlidingWindow => AcquireSlidingWindow(permits, utcNow),
                RateLimiterAlgorithm.LeakyBucket => AcquireLeakyBucket(permits, utcNow),
                _ => throw new InvalidOperationException("Unknown rate limiter algorithm.")
            };

            if (lease.IsAcquired)
                _logger.LogDebug("Rate limiter granted {Permits} permits for {Context}.", permits, context);
            else
                _logger.LogDebug("Rate limiter rejected {Permits} permits for {Context}. Retry after {RetryAfter}.", permits, context, lease.RetryAfter);

            return lease;
        }
    }

    private RateLimitLease AcquireTokenBucket(int permits, DateTimeOffset utcNow)
    {
        var elapsed = utcNow - _tokenBucketRefillAt;
        if (elapsed > TimeSpan.Zero)
        {
            var refillPerSecond = _options.PermitLimit / _options.Window.TotalSeconds;
            _tokenBucketTokens = Math.Min(_options.PermitLimit, _tokenBucketTokens + elapsed.TotalSeconds * refillPerSecond);
            _tokenBucketRefillAt = utcNow;
        }

        if (_tokenBucketTokens < permits)
        {
            var deficit = permits - _tokenBucketTokens;
            var retryAfter = TimeSpan.FromSeconds(deficit * _options.Window.TotalSeconds / _options.PermitLimit);
            return RateLimitLease.Rejected(retryAfter, (int)Math.Floor(_tokenBucketTokens));
        }

        _tokenBucketTokens -= permits;
        return RateLimitLease.Acquired((int)Math.Floor(_tokenBucketTokens));
    }

    private RateLimitLease AcquireFixedWindow(int permits, DateTimeOffset utcNow)
    {
        if (utcNow - _fixedWindowStart >= _options.Window)
        {
            _fixedWindowStart = utcNow;
            _fixedWindowCount = 0;
        }

        if (_fixedWindowCount + permits > _options.PermitLimit)
            return RateLimitLease.Rejected(_options.Window - (utcNow - _fixedWindowStart), Math.Max(0, _options.PermitLimit - _fixedWindowCount));

        _fixedWindowCount += permits;
        return RateLimitLease.Acquired(Math.Max(0, _options.PermitLimit - _fixedWindowCount));
    }

    private RateLimitLease AcquireSlidingWindow(int permits, DateTimeOffset utcNow)
    {
        var segmentDuration = TimeSpan.FromTicks(_options.Window.Ticks / _options.Segments);
        var elapsed = utcNow - _slidingWindowStart;

        if (elapsed >= _options.Window)
        {
            Array.Clear(_slidingWindowSegments, 0, _slidingWindowSegments.Length);
            _slidingWindowStart = utcNow;
            _slidingWindowCurrentSegment = 0;
        }
        else if (elapsed > TimeSpan.Zero)
        {
            var segmentsToAdvance = (int)(elapsed.Ticks / segmentDuration.Ticks);
            for (var i = 0; i < segmentsToAdvance; i++)
            {
                _slidingWindowCurrentSegment = (_slidingWindowCurrentSegment + 1) % _slidingWindowSegments.Length;
                _slidingWindowSegments[_slidingWindowCurrentSegment] = 0;
            }

            if (segmentsToAdvance > 0)
                _slidingWindowStart = _slidingWindowStart.AddTicks(segmentDuration.Ticks * segmentsToAdvance);
        }

        var total = 0;
        for (var i = 0; i < _slidingWindowSegments.Length; i++)
            total += _slidingWindowSegments[i];

        if (total + permits > _options.PermitLimit)
            return RateLimitLease.Rejected(segmentDuration - (utcNow - _slidingWindowStart), Math.Max(0, _options.PermitLimit - total));

        _slidingWindowSegments[_slidingWindowCurrentSegment] += permits;
        return RateLimitLease.Acquired(Math.Max(0, _options.PermitLimit - total - permits));
    }

    private RateLimitLease AcquireLeakyBucket(int permits, DateTimeOffset utcNow)
    {
        var elapsed = utcNow - _leakyBucketDrainAt;
        if (elapsed > TimeSpan.Zero)
        {
            var drainPerSecond = _options.QueueLimit / _options.Window.TotalSeconds;
            _leakyBucketLevel = Math.Max(0, _leakyBucketLevel - elapsed.TotalSeconds * drainPerSecond);
            _leakyBucketDrainAt = utcNow;
        }

        if (_leakyBucketLevel + permits > _options.QueueLimit)
            return RateLimitLease.Rejected(TimeSpan.FromSeconds((_leakyBucketLevel + permits - _options.QueueLimit) * _options.Window.TotalSeconds / _options.QueueLimit), Math.Max(0, _options.QueueLimit - (int)Math.Ceiling(_leakyBucketLevel)));

        _leakyBucketLevel += permits;
        return RateLimitLease.Acquired(Math.Max(0, _options.QueueLimit - (int)Math.Ceiling(_leakyBucketLevel)));
    }
}
