using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.RuleEngine.Abstractions;
using Usm.Shared.Patterns.RuleEngine.Builders;
using Usm.Shared.Patterns.RuleEngine.Configuration;

namespace Usm.Shared.Patterns.RuleEngine.Extensions;

/// <summary>
/// Common extension methods for rule engine creation and DI registration.
/// </summary>
public static class RuleEngineExtensions
{
    /// <summary>Registers the rule engine framework with dependency injection.</summary>
    public static IServiceCollection AddRuleEngineFramework(
        this IServiceCollection services,
        Action<RuleEngineOptions>? configure = null)
    {
        services.AddOptions<RuleEngineOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton(typeof(IRuleCompiler<,>), typeof(RuleCompiler<,>));
        services.TryAddTransient(typeof(RuleBuilder<,>), typeof(RuleBuilder<,>));

        return services;
    }
}

/// <summary>
/// Compiles rule engines and caches expression-based delegates when configured.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class RuleCompiler<TContext, TResult> : IRuleCompiler<TContext, TResult>
{
    private readonly RuleEngineOptions _options;
    private readonly ILogger<RuleCompiler<TContext, TResult>> _logger;
    private readonly ConcurrentDictionary<string, Func<TContext, TResult>> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new compiler.</summary>
    public RuleCompiler(IOptions<RuleEngineOptions> options, ILogger<RuleCompiler<TContext, TResult>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RuleCompiler<TContext, TResult>>.Instance;
    }

    /// <inheritdoc />
    public Func<TContext, TResult> Compile(IRuleEngine<TContext, TResult> engine, string? group = null)
    {
        if (!engine.CanExecuteSynchronously)
            throw new NotSupportedException("The rule engine cannot be compiled to a synchronous delegate.");

        if (!_options.CacheCompiledExpressions || !engine.CanConvertToExpression)
            return context => engine.Evaluate(context, group);

        var expression = engine.ToExpression(group);
        var key = $"{group ?? string.Empty}:{expression}";
        return _cache.GetOrAdd(key, _ =>
        {
            _logger.LogDebug("Caching compiled rule engine for {Type}.", typeof(TContext).FullName);
            return expression.Compile();
        });
    }
}
