using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Strategy.Abstractions;
using Usm.Shared.Patterns.Strategy.Builders;
using Usm.Shared.Patterns.Strategy.Configuration;

namespace Usm.Shared.Patterns.Strategy.Extensions;

/// <summary>
/// Common extension methods for strategy creation and DI registration.
/// </summary>
public static class StrategyExtensions
{
    /// <summary>Converts an expression tree to a strategy.</summary>
    public static IStrategy<TContext, TResult> ToStrategy<TContext, TResult>(
        this Expression<Func<TContext, TResult>> strategy)
        => Strategy<TContext, TResult>.From(strategy);

    /// <summary>Converts a synchronous delegate to a strategy.</summary>
    public static IStrategy<TContext, TResult> ToStrategyPredicate<TContext, TResult>(
        this Func<TContext, TResult> strategy)
        => Strategy<TContext, TResult>.FromPredicate(strategy);

    /// <summary>Converts an asynchronous delegate to a strategy.</summary>
    public static IStrategy<TContext, TResult> ToAsyncStrategy<TContext, TResult>(
        this Func<TContext, CancellationToken, ValueTask<TResult>> strategy)
        => Strategy<TContext, TResult>.FromAsync(strategy);

    /// <summary>Registers the strategy framework with dependency injection.</summary>
    public static IServiceCollection AddStrategyFramework(
        this IServiceCollection services,
        Action<StrategyOptions>? configure = null)
    {
        services.AddOptions<StrategyOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton(typeof(IStrategyCompiler<,>), typeof(StrategyCompiler<,>));
        services.TryAddTransient(typeof(StrategyBuilder<,>), typeof(StrategyBuilder<,>));

        return services;
    }
}

/// <summary>
/// Compiles expression-backed strategies and optionally caches the compiled delegates.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class StrategyCompiler<TContext, TResult> : IStrategyCompiler<TContext, TResult>
{
    private readonly StrategyOptions _options;
    private readonly ILogger<StrategyCompiler<TContext, TResult>> _logger;
    private readonly ConcurrentDictionary<string, Func<TContext, TResult>> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new compiler.</summary>
    public StrategyCompiler(
        IOptions<StrategyOptions> options,
        ILogger<StrategyCompiler<TContext, TResult>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StrategyCompiler<TContext, TResult>>.Instance;
    }

    /// <inheritdoc />
    public Func<TContext, TResult> Compile(IStrategy<TContext, TResult> strategy)
    {
        if (!strategy.CanExecuteSynchronously)
            throw new NotSupportedException("The strategy cannot be compiled to a synchronous delegate.");

        if (!_options.CacheCompiledExpressions || !strategy.CanConvertToExpression)
            return strategy.Compile();

        var expression = strategy.ToExpression();
        var key = expression.ToString();

        return _cache.GetOrAdd(key, _ =>
        {
            TrimCacheIfNeeded();
            _logger.LogDebug("Caching compiled strategy for {Type}.", typeof(TContext).FullName);
            return expression.Compile();
        });
    }

    private void TrimCacheIfNeeded()
    {
        if (_options.CacheCapacity is not int capacity || capacity <= 0)
            return;

        if (_cache.Count < capacity)
            return;

        _cache.Clear();
    }
}
