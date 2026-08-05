using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Strategy.Abstractions;

/// <summary>
/// Fluent builder for composing reusable strategies.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IStrategyBuilder<TContext, TResult>
{
    /// <summary>Adds an expression-based strategy.</summary>
    IStrategyBuilder<TContext, TResult> UseExpression(Expression<Func<TContext, TResult>> strategy);

    /// <summary>Adds a synchronous strategy delegate.</summary>
    IStrategyBuilder<TContext, TResult> UsePredicate(Func<TContext, TResult> strategy);

    /// <summary>Adds an asynchronous strategy delegate.</summary>
    IStrategyBuilder<TContext, TResult> UseAsync(Func<TContext, CancellationToken, ValueTask<TResult>> strategy);

    /// <summary>Builds the configured strategy.</summary>
    IStrategy<TContext, TResult> Build();
}
