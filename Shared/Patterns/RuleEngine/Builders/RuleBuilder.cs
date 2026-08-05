using System.Linq.Expressions;
using Usm.Shared.Patterns.RuleEngine.Abstractions;

namespace Usm.Shared.Patterns.RuleEngine.Builders;

/// <summary>
/// Fluent builder for constructing an ordered rule engine.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class RuleBuilder<TContext, TResult> : IRuleBuilder<TContext, TResult>
{
    private readonly List<RuleDefinition<TContext, TResult>> _rules = [];
    private long _sequence;
    private FallbackDefinition<TContext, TResult>? _fallback;

    /// <inheritdoc />
    public IRuleBuilder<TContext, TResult> WhenExpression(
        Expression<Func<TContext, bool>> predicate,
        Expression<Func<TContext, TResult>> result,
        int priority = 0,
        string? group = null)
    {
        _rules.Add(RuleDefinition<TContext, TResult>.FromExpression(predicate, result, priority, group, _sequence++));
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<TContext, TResult> WhenPredicate(
        Func<TContext, bool> predicate,
        Func<TContext, TResult> result,
        int priority = 0,
        string? group = null)
    {
        _rules.Add(RuleDefinition<TContext, TResult>.FromPredicate(predicate, result, priority, group, _sequence++));
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<TContext, TResult> WhenAsync(
        Func<TContext, CancellationToken, ValueTask<bool>> predicate,
        Func<TContext, CancellationToken, ValueTask<TResult>> result,
        int priority = 0,
        string? group = null)
    {
        _rules.Add(RuleDefinition<TContext, TResult>.FromAsync(predicate, result, priority, group, _sequence++));
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<TContext, TResult> OtherwiseExpression(Expression<Func<TContext, TResult>> result, string? group = null)
    {
        _fallback = FallbackDefinition<TContext, TResult>.FromExpression(result, group);
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<TContext, TResult> OtherwisePredicate(Func<TContext, TResult> result, string? group = null)
    {
        _fallback = FallbackDefinition<TContext, TResult>.FromPredicate(result, group);
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<TContext, TResult> OtherwiseAsync(Func<TContext, CancellationToken, ValueTask<TResult>> result, string? group = null)
    {
        _fallback = FallbackDefinition<TContext, TResult>.FromAsync(result, group);
        return this;
    }

    /// <inheritdoc />
    public IRuleEngine<TContext, TResult> Build()
        => new CompositeRuleEngine<TContext, TResult>(_rules, _fallback);
}
