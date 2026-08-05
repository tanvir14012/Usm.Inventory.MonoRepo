namespace Shared.AI.Python;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Starts and stops the Python worker pool with the application host.
/// </summary>
internal sealed class PythonAIHostedService : IHostedService
{
    private readonly IPythonProcessManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonAIHostedService"/> class.
    /// </summary>
    public PythonAIHostedService(IPythonProcessManager manager)
    {
        _manager = manager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _manager.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _manager.StopAsync(cancellationToken);
    }
}

/// <summary>
/// ASP.NET health check for the Python AI runtime.
/// </summary>
public sealed class PythonAIHealthCheck : IHealthCheck
{
    private readonly IPythonProcessManager _manager;
    private readonly ILogger<PythonAIHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonAIHealthCheck"/> class.
    /// </summary>
    public PythonAIHealthCheck(IPythonProcessManager manager, ILogger<PythonAIHealthCheck> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = _manager.GetSnapshot();
        if (!snapshot.Started)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Python AI runtime has not started."));
        }

        if (snapshot.HealthyWorkers == 0)
        {
            _logger.LogWarning("Python AI health check failed: no healthy workers.");
            return Task.FromResult(HealthCheckResult.Unhealthy("No healthy Python workers are available."));
        }

        if (snapshot.LastError is not null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(snapshot.LastError));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Python AI runtime is healthy."));
    }
}

