namespace Usm.Shared.Patterns.Sidecar.Models;

/// <summary>
/// Possible states of the sidecar's internal circuit breaker.
/// </summary>
public enum SidecarCircuitState
{
    /// <summary>Normal operation — calls are forwarded to the primary.</summary>
    Closed = 0,

    /// <summary>Circuit is open — calls are rejected immediately.</summary>
    Open = 1,

    /// <summary>Limited trial calls are allowed to probe recovery.</summary>
    HalfOpen = 2
}
