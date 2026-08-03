namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>
/// Thread-safe circuit-breaker state machine for a single storage provider.
///
/// States:
///   Closed   → normal operation; all requests pass through.
///   Open     → too many consecutive failures; requests are rejected immediately.
///   HalfOpen → the open duration has elapsed; one probe request is permitted.
///              Success → Closed.  Failure → Open (reset timer).
/// </summary>
internal sealed class CircuitBreakerState
{
    private int _failureCount;
    private DateTimeOffset _openedAt;
    private CircuitState _state = CircuitState.Closed;

    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly Lock _lock = new();

    public CircuitBreakerState(int failureThreshold, TimeSpan openDuration)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }

    /// <summary>Returns true if a request is allowed to proceed to the provider.</summary>
    public bool IsAllowed()
    {
        lock (_lock)
        {
            return _state switch
            {
                CircuitState.Closed => true,
                CircuitState.Open when DateTimeOffset.UtcNow - _openedAt >= _openDuration => TryHalfOpen(),
                CircuitState.HalfOpen => true,
                _ => false
            };
        }
    }

    /// <summary>Record a successful call; resets the circuit to Closed.</summary>
    public void OnSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>Record a failed call; trips the circuit when the threshold is reached.</summary>
    public void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            if (_failureCount >= _failureThreshold || _state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _openedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private bool TryHalfOpen()
    {
        _state = CircuitState.HalfOpen;
        return true;
    }

    private enum CircuitState { Closed, Open, HalfOpen }
}
