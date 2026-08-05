using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Pipeline.Abstractions;
using Usm.Shared.Patterns.Pipeline.Builders;
using Usm.Shared.Patterns.Pipeline.Configuration;

namespace Usm.Shared.Patterns.Pipeline.Extensions;

/// <summary>
/// Common extension methods for pipeline creation and DI registration.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>Creates a pipeline from an expression tree.</summary>
    public static IPipeline<TContext> ToPipeline<TContext>(this Expression<Func<TContext, TContext>> step)
        => Pipeline<TContext>.From(step);

    /// <summary>Registers the pipeline framework with dependency injection.</summary>
    public static IServiceCollection AddPipelineFramework(
        this IServiceCollection services,
        Action<PipelineOptions>? configure = null)
    {
        services.AddOptions<PipelineOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton(typeof(IPipelineCompiler<>), typeof(PipelineCompiler<>));
        services.TryAddTransient(typeof(PipelineBuilder<>), typeof(PipelineBuilder<>));

        return services;
    }
}

/// <summary>
/// Compiles expression-backed pipelines and optionally caches the compiled delegates.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public sealed class PipelineCompiler<TContext> : IPipelineCompiler<TContext>
{
    private readonly PipelineOptions _options;
    private readonly ILogger<PipelineCompiler<TContext>> _logger;
    private readonly ConcurrentDictionary<string, Func<TContext, TContext>> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new compiler.</summary>
    public PipelineCompiler(
        IOptions<PipelineOptions> options,
        ILogger<PipelineCompiler<TContext>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineCompiler<TContext>>.Instance;
    }

    /// <inheritdoc />
    public Func<TContext, TContext> Compile(IPipeline<TContext> pipeline)
    {
        if (!pipeline.CanExecuteSynchronously)
            throw new NotSupportedException("The pipeline cannot be compiled to a synchronous delegate.");

        if (!_options.CacheCompiledExpressions || !pipeline.CanConvertToExpression)
            return pipeline.Compile();

        var expression = pipeline.ToExpression();
        var key = expression.ToString();

        return _cache.GetOrAdd(key, _ =>
        {
            TrimCacheIfNeeded();
            _logger.LogDebug("Caching compiled pipeline for {Type}.", typeof(TContext).FullName);
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
