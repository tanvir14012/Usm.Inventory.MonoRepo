using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Factory.Abstractions;
using Usm.Shared.Patterns.Factory.Builders;
using Usm.Shared.Patterns.Factory.Configuration;

namespace Usm.Shared.Patterns.Factory.Extensions;

/// <summary>
/// Common extension methods for factory creation and DI registration.
/// </summary>
public static class FactoryExtensions
{
    /// <summary>Converts an expression tree to a factory.</summary>
    public static IFactory<TContext, TProduct> ToFactory<TContext, TProduct>(
        this Expression<Func<TContext, TProduct>> factory)
        => Factory<TContext, TProduct>.From(factory);

    /// <summary>Converts a synchronous delegate to a factory.</summary>
    public static IFactory<TContext, TProduct> ToFactoryPredicate<TContext, TProduct>(
        this Func<TContext, TProduct> factory)
        => Factory<TContext, TProduct>.FromPredicate(factory);

    /// <summary>Converts an async delegate to a factory.</summary>
    public static IFactory<TContext, TProduct> ToAsyncFactory<TContext, TProduct>(
        this Func<TContext, CancellationToken, ValueTask<TProduct>> factory)
        => Factory<TContext, TProduct>.FromAsync(factory);

    /// <summary>Registers the factory framework with dependency injection.</summary>
    public static IServiceCollection AddFactoryFramework(
        this IServiceCollection services,
        Action<FactoryOptions>? configure = null)
    {
        services.AddOptions<FactoryOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton(typeof(IFactoryCompiler<,>), typeof(FactoryCompiler<,>));
        services.TryAddTransient(typeof(FactoryBuilder<,>), typeof(FactoryBuilder<,>));

        return services;
    }
}

/// <summary>
/// Compiles expression-backed factories and optionally caches the compiled delegates.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TProduct">The produced type.</typeparam>
public sealed class FactoryCompiler<TContext, TProduct> : IFactoryCompiler<TContext, TProduct>
{
    private readonly FactoryOptions _options;
    private readonly ILogger<FactoryCompiler<TContext, TProduct>> _logger;
    private readonly ConcurrentDictionary<string, Func<TContext, TProduct>> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new compiler.</summary>
    public FactoryCompiler(
        IOptions<FactoryOptions> options,
        ILogger<FactoryCompiler<TContext, TProduct>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<FactoryCompiler<TContext, TProduct>>.Instance;
    }

    /// <inheritdoc />
    public Func<TContext, TProduct> Compile(IFactory<TContext, TProduct> factory)
    {
        if (!factory.CanCreateSynchronously)
            throw new NotSupportedException("The factory cannot be compiled to a synchronous delegate.");

        if (!_options.CacheCompiledExpressions || !factory.CanConvertToExpression)
            return factory.Compile();

        var expression = factory.ToExpression();
        var key = expression.ToString();

        return _cache.GetOrAdd(key, _ =>
        {
            TrimCacheIfNeeded();
            _logger.LogDebug("Caching compiled factory for {Type}.", typeof(TContext).FullName);
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
