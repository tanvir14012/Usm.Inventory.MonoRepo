using System.Linq.Expressions;
using Usm.Shared.Patterns.Strategy.Abstractions;

namespace Usm.Shared.Patterns.Strategy.Builders;

/// <summary>
/// Fluent builder for constructing a strategy.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public sealed class StrategyBuilder<TContext, TResult> : IStrategyBuilder<TContext, TResult>
{
    private IStrategy<TContext, TResult>? _current;

    /// <inheritdoc />
    public IStrategyBuilder<TContext, TResult> UseExpression(Expression<Func<TContext, TResult>> strategy)
    {
        _current = Strategy<TContext, TResult>.From(strategy);
        return this;
    }

    /// <inheritdoc />
    public IStrategyBuilder<TContext, TResult> UsePredicate(Func<TContext, TResult> strategy)
    {
        _current = Strategy<TContext, TResult>.FromPredicate(strategy);
        return this;
    }

    /// <inheritdoc />
    public IStrategyBuilder<TContext, TResult> UseAsync(Func<TContext, CancellationToken, ValueTask<TResult>> strategy)
    {
        _current = Strategy<TContext, TResult>.FromAsync(strategy);
        return this;
    }

    /// <inheritdoc />
    public IStrategy<TContext, TResult> Build()
        => _current ?? throw new InvalidOperationException("No strategy has been configured.");
}
