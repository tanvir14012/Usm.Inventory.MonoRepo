namespace Shared.AI.Core;

using Microsoft.Extensions.Logging;

/// <summary>
/// Routes requests to the best-suited LLM provider based on criteria.
/// </summary>
public class ModelRouter
{
    private readonly Dictionary<string, ProviderMetrics> _providers = new();
    private readonly ILogger? _logger;
    private readonly ReaderWriterLockSlim _lock = new();

    public ModelRouter(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a provider with routing configuration.
    /// </summary>
    public void RegisterProvider(
        string name,
        ILLMProvider provider,
        ProviderConfig config)
    {
        _lock.EnterWriteLock();
        try
        {
            _providers[name] = new ProviderMetrics
            {
                Name = name,
                Provider = provider,
                Config = config,
                SuccessRate = 1.0,
                AverageLatency = 0,
                CostPerToken = config.CostPerToken,
                LastUsed = DateTime.UtcNow,
                IsAvailable = true
            };

            _logger?.LogInformation("Provider registered: {Name}", name);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Selects the best provider based on routing strategy.
    /// </summary>
    public ILLMProvider SelectProvider(RoutingStrategy strategy)
    {
        _lock.EnterReadLock();
        try
        {
            var availableProviders = _providers.Values
                .Where(p => p.IsAvailable)
                .ToList();

            if (!availableProviders.Any())
                throw new InvalidOperationException("No available providers");

            return strategy switch
            {
                RoutingStrategy.CostOptimal => availableProviders
                    .OrderBy(p => p.CostPerToken)
                    .First()
                    .Provider,

                RoutingStrategy.LowestLatency => availableProviders
                    .OrderBy(p => p.AverageLatency)
                    .First()
                    .Provider,

                RoutingStrategy.HighestAccuracy => availableProviders
                    .OrderByDescending(p => p.SuccessRate)
                    .First()
                    .Provider,

                RoutingStrategy.RoundRobin => availableProviders
                    .OrderBy(p => p.LastUsed)
                    .First()
                    .Provider,

                RoutingStrategy.Fallback => availableProviders.First().Provider,

                _ => throw new ArgumentException("Unknown routing strategy")
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Selects providers for multi-provider failover.
    /// </summary>
    public IReadOnlyList<ILLMProvider> SelectProviderChain(int count, RoutingStrategy strategy)
    {
        _lock.EnterReadLock();
        try
        {
            var availableProviders = _providers.Values
                .Where(p => p.IsAvailable)
                .ToList();

            if (availableProviders.Count < count)
                throw new InvalidOperationException($"Only {availableProviders.Count} providers available, requested {count}");

            var chain = strategy switch
            {
                RoutingStrategy.CostOptimal => availableProviders
                    .OrderBy(p => p.CostPerToken)
                    .Take(count)
                    .Select(p => p.Provider)
                    .ToList(),

                RoutingStrategy.LowestLatency => availableProviders
                    .OrderBy(p => p.AverageLatency)
                    .Take(count)
                    .Select(p => p.Provider)
                    .ToList(),

                RoutingStrategy.HighestAccuracy => availableProviders
                    .OrderByDescending(p => p.SuccessRate)
                    .Take(count)
                    .Select(p => p.Provider)
                    .ToList(),

                _ => availableProviders
                    .Take(count)
                    .Select(p => p.Provider)
                    .ToList()
            };

            return chain;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Records a successful request for metrics.
    /// </summary>
    public void RecordSuccess(string providerName, TimeSpan latency, int tokensUsed = 0)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_providers.TryGetValue(providerName, out var metrics))
            {
                metrics.SuccessCount++;
                metrics.TotalTokens += tokensUsed;
                
                var oldLatency = metrics.AverageLatency;
                var oldCount = metrics.SuccessCount - 1;
                metrics.AverageLatency = (oldLatency * oldCount + latency.TotalMilliseconds) / metrics.SuccessCount;
                
                metrics.LastUsed = DateTime.UtcNow;
                UpdateSuccessRate(metrics);

                _logger?.LogDebug("Provider {Name} success recorded: latency {Latency}ms", 
                    providerName, latency.TotalMilliseconds);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Records a failed request for metrics.
    /// </summary>
    public void RecordFailure(string providerName)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_providers.TryGetValue(providerName, out var metrics))
            {
                metrics.FailureCount++;
                UpdateSuccessRate(metrics);

                // Disable provider if failure rate exceeds threshold
                if (metrics.SuccessRate < 0.5)
                {
                    metrics.IsAvailable = false;
                    _logger?.LogWarning("Provider {Name} disabled due to high failure rate", providerName);
                }
                else
                {
                    _logger?.LogDebug("Provider {Name} failure recorded", providerName);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets metrics for a provider.
    /// </summary>
    public ProviderMetrics? GetMetrics(string providerName)
    {
        _lock.EnterReadLock();
        try
        {
            return _providers.TryGetValue(providerName, out var metrics) ? metrics : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets all provider metrics.
    /// </summary>
    public IReadOnlyList<ProviderMetrics> GetAllMetrics()
    {
        _lock.EnterReadLock();
        try
        {
            return _providers.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private static void UpdateSuccessRate(ProviderMetrics metrics)
    {
        var total = metrics.SuccessCount + metrics.FailureCount;
        metrics.SuccessRate = total > 0 ? (double)metrics.SuccessCount / total : 1.0;
    }
}

/// <summary>
/// Provider configuration for routing.
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Cost per token (USD)
    /// </summary>
    public decimal CostPerToken { get; set; }

    /// <summary>
    /// Maximum tokens per request
    /// </summary>
    public int MaxTokensPerRequest { get; set; }

    /// <summary>
    /// Model capabilities (json, vision, etc.)
    /// </summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>
    /// Maximum requests per minute
    /// </summary>
    public int RateLimit { get; set; }

    /// <summary>
    /// Supported languages
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new();
}

/// <summary>
/// Metrics for provider performance tracking.
/// </summary>
public class ProviderMetrics
{
    public string? Name { get; set; }
    public ILLMProvider? Provider { get; set; }
    public ProviderConfig? Config { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatency { get; set; }
    public decimal CostPerToken { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsAvailable { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalTokens { get; set; }

    public decimal TotalCost => TotalTokens * CostPerToken;
}

/// <summary>
/// Routing strategies for provider selection.
/// </summary>
public enum RoutingStrategy
{
    /// <summary>
    /// Select the cheapest provider
    /// </summary>
    CostOptimal,

    /// <summary>
    /// Select the fastest provider
    /// </summary>
    LowestLatency,

    /// <summary>
    /// Select the most reliable provider
    /// </summary>
    HighestAccuracy,

    /// <summary>
    /// Distribute evenly across providers
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Use first available provider
    /// </summary>
    Fallback
}

/// <summary>
/// Context-aware provider selection based on request requirements.
/// </summary>
public class ContextAwareRouter
{
    private readonly ModelRouter _router;
    private readonly ILogger? _logger;

    public ContextAwareRouter(ModelRouter router, ILogger? logger = null)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger;
    }

    /// <summary>
    /// Selects provider based on request context.
    /// </summary>
    public ILLMProvider SelectForContext(RequestContext context)
    {
        return context.Priority switch
        {
            RequestPriority.Cost => _router.SelectProvider(RoutingStrategy.CostOptimal),
            RequestPriority.Speed => _router.SelectProvider(RoutingStrategy.LowestLatency),
            RequestPriority.Accuracy => _router.SelectProvider(RoutingStrategy.HighestAccuracy),
            _ => _router.SelectProvider(RoutingStrategy.RoundRobin)
        };
    }

    /// <summary>
    /// Selects a fallback chain for retries.
    /// </summary>
    public IReadOnlyList<ILLMProvider> SelectFallbackChain(RequestContext context, int chainLength = 3)
    {
        return context.Priority switch
        {
            RequestPriority.Cost => _router.SelectProviderChain(chainLength, RoutingStrategy.CostOptimal),
            RequestPriority.Speed => _router.SelectProviderChain(chainLength, RoutingStrategy.LowestLatency),
            RequestPriority.Accuracy => _router.SelectProviderChain(chainLength, RoutingStrategy.HighestAccuracy),
            _ => _router.SelectProviderChain(chainLength, RoutingStrategy.Fallback)
        };
    }
}

/// <summary>
/// Request context for routing decisions.
/// </summary>
public class RequestContext
{
    public RequestPriority Priority { get; set; } = RequestPriority.Balanced;
    public int EstimatedTokens { get; set; }
    public bool RequiresVision { get; set; }
    public bool RequiresJson { get; set; }
    public List<string>? RequiredCapabilities { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Request priority for provider selection.
/// </summary>
public enum RequestPriority
{
    Cost,
    Speed,
    Accuracy,
    Balanced
}
