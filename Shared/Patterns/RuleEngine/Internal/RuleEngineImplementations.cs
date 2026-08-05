using System.Collections.Concurrent;
using System.Linq.Expressions;
using Usm.Shared.Patterns.RuleEngine.Abstractions;
using Usm.Shared.Patterns.RuleEngine.Builders;

namespace Usm.Shared.Patterns.RuleEngine;

/// <summary>
/// Base type for reusable rule engines.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public abstract class RuleEngine<TContext, TResult> : IRuleEngine<TContext, TResult>
{
    /// <summary>Creates a builder for configuring a rule engine.</summary>
    public static RuleBuilder<TContext, TResult> CreateBuilder()
        => new();

    /// <inheritdoc />
    public abstract bool CanExecuteSynchronously { get; }

    /// <inheritdoc />
    public abstract bool CanConvertToExpression { get; }

    /// <inheritdoc />
    public abstract TResult Evaluate(TContext context, string? group = null);

    /// <inheritdoc />
    public abstract ValueTask<TResult> EvaluateAsync(TContext context, string? group = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Expression<Func<TContext, TResult>> ToExpression(string? group = null);

    /// <inheritdoc />
    public virtual Func<TContext, TResult> Compile(string? group = null)
        => ToExpression(group).Compile();
}

internal sealed class CompositeRuleEngine<TContext, TResult> : RuleEngine<TContext, TResult>
{
    private readonly IReadOnlyList<RuleDefinition<TContext, TResult>> _rules;
    private readonly FallbackDefinition<TContext, TResult>? _fallback;
    private readonly ConcurrentDictionary<string, Lazy<Func<TContext, TResult>>> _compiledCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Expression<Func<TContext, TResult>>>> _expressionCache = new(StringComparer.Ordinal);

    public CompositeRuleEngine(
        IReadOnlyList<RuleDefinition<TContext, TResult>> rules,
        FallbackDefinition<TContext, TResult>? fallback)
    {
        _rules = rules.OrderByDescending(rule => rule.Priority).ThenBy(rule => rule.Sequence).ToArray();
        _fallback = fallback;
    }

    public override bool CanExecuteSynchronously
        => _rules.All(rule => rule.CanExecuteSynchronously) && (_fallback?.CanExecuteSynchronously ?? true);

    public override bool CanConvertToExpression
        => _rules.All(rule => rule.CanConvertToExpression) && (_fallback?.CanConvertToExpression ?? true);

    public override TResult Evaluate(TContext context, string? group = null)
    {
        if (!CanExecuteSynchronously)
            throw new NotSupportedException("This rule engine requires asynchronous execution.");

        foreach (var rule in FilterRules(group))
        {
            if (rule.Predicate(context))
                return rule.Result(context);
        }

        if (_fallback is not null && MatchesGroup(_fallback.Group, group))
            return _fallback.Result(context);

        if (_fallback is not null)
            throw new InvalidOperationException($"No rule matched the requested group '{group}'.");

        throw new InvalidOperationException("No rule matched the supplied context.");
    }

    public override async ValueTask<TResult> EvaluateAsync(TContext context, string? group = null, CancellationToken cancellationToken = default)
    {
        foreach (var rule in FilterRules(group))
        {
            if (await rule.PredicateAsync(context, cancellationToken).ConfigureAwait(false))
                return rule.CanExecuteSynchronously ? rule.Result(context) : await rule.ResultAsync(context, cancellationToken).ConfigureAwait(false);
        }

        if (_fallback is not null && MatchesGroup(_fallback.Group, group))
        {
            return _fallback.CanExecuteSynchronously
                ? _fallback.Result(context)
                : await _fallback.ResultAsync(context, cancellationToken).ConfigureAwait(false);
        }

        if (_fallback is not null)
            throw new InvalidOperationException($"No rule matched the requested group '{group}'.");

        throw new InvalidOperationException("No rule matched the supplied context.");
    }

    public override Expression<Func<TContext, TResult>> ToExpression(string? group = null)
    {
        if (!CanConvertToExpression)
            throw new NotSupportedException("This rule engine cannot be converted to an expression tree.");

        var cacheKey = group ?? string.Empty;
        return _expressionCache.GetOrAdd(cacheKey, _ => new Lazy<Expression<Func<TContext, TResult>>>(() => BuildExpression(group), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public override Func<TContext, TResult> Compile(string? group = null)
    {
        if (!CanExecuteSynchronously)
            throw new NotSupportedException("The rule engine cannot be compiled to a synchronous delegate.");

        var cacheKey = group ?? string.Empty;
        return _compiledCache.GetOrAdd(cacheKey, _ => new Lazy<Func<TContext, TResult>>(() => ToExpression(group).Compile(), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private Expression<Func<TContext, TResult>> BuildExpression(string? group)
    {
        var parameter = Expression.Parameter(typeof(TContext), "context");
        Expression body = BuildFailureExpression();

        if (_fallback is not null && MatchesGroup(_fallback.Group, group))
            body = InvokeResultExpression(_fallback.Result, parameter);

        foreach (var rule in FilterRules(group).Reverse())
        {
            var predicate = InvokePredicateExpression(rule.Predicate, parameter);
            var result = InvokeResultExpression(rule.Result, parameter);
            body = Expression.Condition(predicate, result, body);
        }

        return Expression.Lambda<Func<TContext, TResult>>(body, parameter);
    }

    private IEnumerable<RuleDefinition<TContext, TResult>> FilterRules(string? group)
        => _rules.Where(rule => MatchesGroup(rule.Group, group));

    private static bool MatchesGroup(string? ruleGroup, string? requestedGroup)
        => string.Equals(ruleGroup, requestedGroup, StringComparison.Ordinal);

    private static Expression InvokePredicateExpression(Func<TContext, bool> predicate, ParameterExpression parameter)
        => Expression.Invoke(Expression.Constant(predicate), parameter);

    private static Expression InvokeResultExpression(Func<TContext, TResult> result, ParameterExpression parameter)
        => Expression.Invoke(Expression.Constant(result), parameter);

    private static Expression BuildFailureExpression()
        => Expression.Throw(
            Expression.New(
                typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                Expression.Constant("No rule matched the supplied context.")),
            typeof(TResult));
}
