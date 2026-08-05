using System.Linq.Expressions;

namespace Usm.Shared.Patterns.RuleEngine.Abstractions;

/// <summary>
/// Fluent builder for constructing a rule engine.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IRuleBuilder<TContext, TResult>
{
    /// <summary>Adds an expression-based rule.</summary>
    IRuleBuilder<TContext, TResult> WhenExpression(
        Expression<Func<TContext, bool>> predicate,
        Expression<Func<TContext, TResult>> result,
        int priority = 0,
        string? group = null);

    /// <summary>Adds a synchronous rule.</summary>
    IRuleBuilder<TContext, TResult> WhenPredicate(
        Func<TContext, bool> predicate,
        Func<TContext, TResult> result,
        int priority = 0,
        string? group = null);

    /// <summary>Adds an asynchronous rule.</summary>
    IRuleBuilder<TContext, TResult> WhenAsync(
        Func<TContext, CancellationToken, ValueTask<bool>> predicate,
        Func<TContext, CancellationToken, ValueTask<TResult>> result,
        int priority = 0,
        string? group = null);

    /// <summary>Adds a fallback result for unmatched contexts.</summary>
    IRuleBuilder<TContext, TResult> OtherwiseExpression(Expression<Func<TContext, TResult>> result, string? group = null);

    /// <summary>Adds a synchronous fallback result for unmatched contexts.</summary>
    IRuleBuilder<TContext, TResult> OtherwisePredicate(Func<TContext, TResult> result, string? group = null);

    /// <summary>Adds an asynchronous fallback result for unmatched contexts.</summary>
    IRuleBuilder<TContext, TResult> OtherwiseAsync(Func<TContext, CancellationToken, ValueTask<TResult>> result, string? group = null);

    /// <summary>Builds the rule engine.</summary>
    IRuleEngine<TContext, TResult> Build();
}
