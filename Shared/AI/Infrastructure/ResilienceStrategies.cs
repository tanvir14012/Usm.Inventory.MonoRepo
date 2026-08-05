namespace Shared.AI.Infrastructure;

using Microsoft.Extensions.Logging;

using Shared.AI.Abstractions;

/// <summary>
/// Represents a retry policy for handling transient failures.
/// Supports exponential backoff, linear backoff, and custom strategies.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Gets the maximum number of attempts.
    /// </summary>
    int MaxAttempts { get; }

    /// <summary>
    /// Determines if an exception should be retried.
    /// </summary>
    bool ShouldRetry(Exception exception, int attemptNumber);

    /// <summary>
    /// Calculates the delay before the next retry.
    /// </summary>
    TimeSpan GetDelay(int attemptNumber);
}

/// <summary>
/// Exponential backoff retry policy with jitter.
/// </summary>
public class ExponentialBackoffPolicy : IRetryPolicy
{
    private readonly Random _random = new();

    public int MaxAttempts { get; }

    /// <summary>Gets the initial delay.</summary>
    public TimeSpan InitialDelay { get; }

    /// <summary>Gets the multiplier for each retry.</summary>
    public double Multiplier { get; }

    /// <summary>Gets the maximum delay between retries.</summary>
    public TimeSpan MaxDelay { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of attempts.</param>
    /// <param name="initialDelay">Initial delay before first retry.</param>
    /// <param name="multiplier">Exponential multiplier (usually 2.0).</param>
    /// <param name="maxDelay">Maximum delay cap.</param>
    public ExponentialBackoffPolicy(
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        double multiplier = 2.0,
        TimeSpan? maxDelay = null)
    {
        MaxAttempts = maxAttempts;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        Multiplier = multiplier;
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(60);
    }

    public bool ShouldRetry(Exception exception, int attemptNumber) =>
        attemptNumber < MaxAttempts && IsTransient(exception);

    public TimeSpan GetDelay(int attemptNumber)
    {
        var exponentialDelay = InitialDelay.TotalSeconds * Math.Pow(Multiplier, attemptNumber - 1);
        var delay = TimeSpan.FromSeconds(Math.Min(exponentialDelay, MaxDelay.TotalSeconds));

        // Add jitter: ±10% of delay
        var jitterAmount = delay.TotalSeconds * 0.1;
        var jitter = (_random.NextDouble() - 0.5) * 2 * jitterAmount;

        return TimeSpan.FromSeconds(Math.Max(0, delay.TotalSeconds + jitter));
    }

    private static bool IsTransient(Exception exception) =>
        exception is
        {
            InnerException: HttpRequestException { StatusCode: System.Net.HttpStatusCode statusCode }
        } when ((int) statusCode >= 500 || (int) statusCode == 429) ||
        exception is TimeoutException ||
        exception is OperationCanceledException ||
        exception?.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true ||
        exception?.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Linear backoff retry policy.
/// </summary>
public class LinearBackoffPolicy : IRetryPolicy
{
    public int MaxAttempts { get; }
    public TimeSpan Delay { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public LinearBackoffPolicy(int maxAttempts = 3, TimeSpan? delay = null)
    {
        MaxAttempts = maxAttempts;
        Delay = delay ?? TimeSpan.FromSeconds(1);
    }

    public bool ShouldRetry(Exception exception, int attemptNumber) =>
        attemptNumber < MaxAttempts;

    public TimeSpan GetDelay(int attemptNumber) => Delay;
}

/// <summary>
/// Interface for fallback strategies when primary operation fails.
/// </summary>
public interface IFallbackStrategy<TRequest, TResponse>
{
    /// <summary>
    /// Executes the fallback logic.
    /// </summary>
    Task<AIResult<TResponse>> ExecuteAsync(
        TRequest request,
        Exception primaryException,
        CancellationToken cancellationToken);
}

/// <summary>
/// Fallback strategy that uses an alternative provider.
/// </summary>
public abstract class ProviderFallbackStrategy<TRequest, TResponse> : IFallbackStrategy<TRequest, TResponse>
{
    /// <summary>
    /// Gets the fallback provider name.
    /// </summary>
    protected abstract string FallbackProviderName { get; }

    /// <summary>
    /// Executes the fallback on an alternative provider.
    /// </summary>
    public abstract Task<AIResult<TResponse>> ExecuteAsync(
        TRequest request,
        Exception primaryException,
        CancellationToken cancellationToken);
}

/// <summary>
/// Fallback strategy that returns a cached or default response.
/// </summary>
public class CacheFallbackStrategy<TRequest, TResponse> : IFallbackStrategy<TRequest, TResponse>
{
    private readonly Func<TRequest, Task<TResponse?>> _getCachedValue;
    private readonly TResponse? _defaultValue;

    public CacheFallbackStrategy(
        Func<TRequest, Task<TResponse?>> getCachedValue,
        TResponse? defaultValue = default)
    {
        _getCachedValue = getCachedValue;
        _defaultValue = defaultValue;
    }

    public async Task<AIResult<TResponse>> ExecuteAsync(
        TRequest request,
        Exception primaryException,
        CancellationToken cancellationToken)
    {
        var cached = await _getCachedValue(request);
        if (cached != null)
            return AIResult<TResponse>.Success(cached);

        if (_defaultValue != null)
            return AIResult<TResponse>.Success(_defaultValue);

        return AIResult<TResponse>.Failure(
            new AIError("No cached value and no default available", "NO_FALLBACK"));
    }
}

/// <summary>
/// Orchestrates retry and fallback strategies.
/// </summary>
public class ResilientExecutor<TRequest, TResponse>
{
    private readonly IRetryPolicy _retryPolicy;
    private readonly List<IFallbackStrategy<TRequest, TResponse>> _fallbacks;
    private readonly Func<TRequest, CancellationToken, Task<AIResult<TResponse>>> _operation;
    private readonly ILogger? _logger;

    public ResilientExecutor(
        Func<TRequest, CancellationToken, Task<AIResult<TResponse>>> operation,
        IRetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        _operation = operation;
        _retryPolicy = retryPolicy ?? new ExponentialBackoffPolicy();
        _fallbacks = new List<IFallbackStrategy<TRequest, TResponse>>();
        _logger = logger;
    }

    /// <summary>
    /// Adds a fallback strategy.
    /// </summary>
    public ResilientExecutor<TRequest, TResponse> WithFallback(
        IFallbackStrategy<TRequest, TResponse> fallback)
    {
        _fallbacks.Add(fallback);
        return this;
    }

    /// <summary>
    /// Executes the operation with retry and fallback.
    /// </summary>
    public async Task<AIResult<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _retryPolicy.MaxAttempts; attempt++)
        {
            try
            {
                var result = await _operation(request, cancellationToken);
                if (result.IsSuccess)
                    return result;

                lastException = new InvalidOperationException(result.GetErrorOrNull()?.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Attempt {Attempt} failed: {Message}", attempt, ex.Message);
                lastException = ex;

                if (!_retryPolicy.ShouldRetry(ex, attempt))
                    break;

                if (attempt < _retryPolicy.MaxAttempts)
                {
                    var delay = _retryPolicy.GetDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // Try fallbacks
        foreach (var fallback in _fallbacks)
        {
            try
            {
                _logger?.LogDebug("Trying fallback strategy");
                var result = await fallback.ExecuteAsync(request, lastException!, cancellationToken);
                if (result.IsSuccess)
                    return result;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Fallback failed: {Message}", ex.Message);
            }
        }

        return AIResult<TResponse>.Failure(
            new AIError("Operation failed after all retries and fallbacks", "EXHAUSTED", lastException));
    }
}
