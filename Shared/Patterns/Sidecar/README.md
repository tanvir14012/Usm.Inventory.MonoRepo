# Sidecar Pattern

Production-grade Sidecar design pattern implementation that transparently wraps a primary service
with cross-cutting resilience concerns: exponential back-off retry, circuit breaker, per-call
execution timeout, structured telemetry, and ASP.NET Core health integration.

## Folder structure

```text
Shared/Patterns/Sidecar
├── Abstractions
│   ├── ISidecar.cs            — core contract
│   ├── ISidecarBuilder.cs     — fluent builder contract
│   └── ISidecarMetrics.cs     — live metrics contract
├── Builders
│   └── SidecarBuilder.cs      — fluent builder implementation
├── Configuration
│   └── SidecarOptions.cs      — all tuneable knobs in one place
├── Extensions
│   ├── SidecarCore.cs         — Sidecar<T> + SidecarMetrics + SidecarCircuitOpenException
│   └── SidecarExtensions.cs   — DI helpers + SidecarHealthCheck<T>
├── Models
│   ├── SidecarCircuitState.cs — enum: Closed | Open | HalfOpen
│   └── SidecarMetricsSnapshot.cs — immutable metrics record
└── README.md
```

## Resilience pipeline (per call)

```
caller
  │
  ▼
[Circuit Breaker guard] ─── Open? ──▶ SidecarCircuitOpenException
  │ Closed / HalfOpen
  ▼
[Retry loop  1..MaxAttempts]
  │
  ├──▶ [Execution Timeout wrapper]
  │         │
  │         ▼
  │    primary.Operation(...)
  │         │
  │   ┌─────┴──────┐
  │   │ success    │ exception / timeout
  │   ▼            ▼
  │ OnSuccess   OnFailure ──▶ update circuit counter
  │   │            │
  │   │         delay (exponential back-off + jitter)
  │   │            │
  │   └────────────┴──▶ next attempt (if attempts remain)
  │
  ▼
result / last exception rethrown
```

## Capabilities

| Feature | Details |
|---|---|
| **Retry** | Configurable max attempts (default 3) |
| **Back-off strategies** | Fixed, Linear, Exponential (default) |
| **Jitter** | Decorrelated jitter to avoid thundering-herd (enabled by default) |
| **Back-off cap** | `RetryMaxDelay` prevents runaway delays (default 30 s) |
| **Circuit Breaker** | Closed → Open → HalfOpen → Closed state machine |
| **Failure threshold** | Consecutive failures before tripping (default 5) |
| **Open duration** | How long the circuit stays open (default 30 s) |
| **Half-open permits** | Trial calls allowed during probe (default 1) |
| **Execution timeout** | Per-call timeout with `TimeoutException` on breach |
| **Metrics** | Lock-free Interlocked counters; `Snapshot()` for point-in-time capture |
| **Health check** | `IHealthCheck` integration (Healthy/Degraded/Unhealthy by circuit state) |
| **DI** | `AddSidecarFramework()` + `AddSidecar<TService, TImpl>()` helpers |
| **Testability** | Pluggable `TimeProvider`; zero-delay supported |

## Quick start

```csharp
// Fluent — standalone (no DI)
var sidecar = new SidecarBuilder<IPaymentGateway>()
    .WithMaxAttempts(4)
    .WithRetryStrategy(SidecarRetryStrategy.Exponential)
    .WithRetryBaseDelay(TimeSpan.FromMilliseconds(200))
    .WithRetryMaxDelay(TimeSpan.FromSeconds(10))
    .WithJitter(true)
    .WithFailureThreshold(5)
    .WithCircuitOpenDuration(TimeSpan.FromSeconds(30))
    .WithExecutionTimeout(TimeSpan.FromSeconds(3))
    .Build(new StripePaymentGateway());

var result = await sidecar.ExecuteAsync(
    async (gateway, ct) => await gateway.ChargeAsync(request, ct));
```

## DI registration

```csharp
builder.Services
    .AddSidecarFramework()
    .AddSidecar<IPaymentGateway, StripePaymentGateway>(options =>
    {
        options.MaxAttempts            = 4;
        options.RetryStrategy          = SidecarRetryStrategy.Exponential;
        options.RetryBaseDelay         = TimeSpan.FromMilliseconds(200);
        options.FailureThreshold       = 5;
        options.CircuitOpenDuration    = TimeSpan.FromSeconds(30);
        options.ExecutionTimeout       = TimeSpan.FromSeconds(3);
    });

// In a handler:
public class ChargeHandler(ISidecar<IPaymentGateway> sidecar)
{
    public Task<Receipt> HandleAsync(ChargeCommand cmd, CancellationToken ct)
        => sidecar.ExecuteAsync((gw, t) => gw.ChargeAsync(cmd, t), ct).AsTask();
}
```

## Health endpoint

The `AddSidecar` overload automatically registers an ASP.NET Core health check named
`<TService>-sidecar`. It maps circuit state → HTTP status:

| Circuit state | Health status |
|---|---|
| Closed | Healthy (200) |
| HalfOpen | Degraded (200*) |
| Open | Unhealthy (503) |

## Complexity

| Operation | Cost |
|---|---|
| Circuit state read | O(1) lock-free read + lock for refresh |
| Call dispatch (happy path) | O(1) excluding user code |
| Retry delay computation | O(1) |
| Metrics record | O(1) lock-free Interlocked |
| Health snapshot | O(1) |
