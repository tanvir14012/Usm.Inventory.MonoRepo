using System.Linq.Expressions;
using Usm.Shared.Patterns.Strategy.Abstractions;
using Usm.Shared.Patterns.Strategy.Builders;

namespace Usm.Shared.Patterns.Strategy;

/// <summary>
/// Base type for reusable strategies.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public abstract class Strategy<TContext, TResult> : IStrategy<TContext, TResult>
{
    /// <summary>Creates a strategy from an expression tree.</summary>
    public static IStrategy<TContext, TResult> From(Expression<Func<TContext, TResult>> strategy)
        => new ExpressionStrategy<TContext, TResult>(strategy);

    /// <summary>Creates a strategy from a synchronous delegate.</summary>
    public static IStrategy<TContext, TResult> FromPredicate(Func<TContext, TResult> strategy)
        => new PredicateStrategy<TContext, TResult>(strategy);

    /// <summary>Creates a strategy from an asynchronous delegate.</summary>
    public static IStrategy<TContext, TResult> FromAsync(Func<TContext, CancellationToken, ValueTask<TResult>> strategy)
        => new AsyncStrategy<TContext, TResult>(strategy);

    /// <summary>Creates a builder for composing strategies.</summary>
    public static StrategyBuilder<TContext, TResult> CreateBuilder()
        => new();

    /// <inheritdoc />
    public virtual bool CanExecuteSynchronously => true;

    /// <inheritdoc />
    public virtual bool CanExecuteAsynchronously => true;

    /// <inheritdoc />
    public virtual bool CanConvertToExpression => true;

    /// <inheritdoc />
    public abstract TResult Execute(TContext context);

    /// <inheritdoc />
    public virtual ValueTask<TResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Execute(context));

    /// <inheritdoc />
    public abstract Expression<Func<TContext, TResult>> ToExpression();

    /// <inheritdoc />
    public virtual Func<TContext, TResult> Compile()
        => ToExpression().Compile();
}

internal sealed class ExpressionStrategy<TContext, TResult> : Strategy<TContext, TResult>
{
    private readonly Expression<Func<TContext, TResult>> _expression;
    private readonly Lazy<Func<TContext, TResult>> _compiled;

    public ExpressionStrategy(Expression<Func<TContext, TResult>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _compiled = new Lazy<Func<TContext, TResult>>(() => _expression.Compile(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override TResult Execute(TContext context)
        => _compiled.Value(context);

    public override Expression<Func<TContext, TResult>> ToExpression()
        => _expression;

    public override Func<TContext, TResult> Compile()
        => _compiled.Value;
}

internal sealed class PredicateStrategy<TContext, TResult> : Strategy<TContext, TResult>
{
    private readonly Func<TContext, TResult> _strategy;
    private readonly Lazy<Expression<Func<TContext, TResult>>> _expression;

    public PredicateStrategy(Func<TContext, TResult> strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _expression = new Lazy<Expression<Func<TContext, TResult>>>(CreateExpression, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override TResult Execute(TContext context)
        => _strategy(context);

    public override Expression<Func<TContext, TResult>> ToExpression()
        => _expression.Value;

    public override Func<TContext, TResult> Compile()
        => _strategy;

    private Expression<Func<TContext, TResult>> CreateExpression()
    {
        var parameter = Expression.Parameter(typeof(TContext), "context");
        var invoke = Expression.Invoke(Expression.Constant(_strategy), parameter);
        return Expression.Lambda<Func<TContext, TResult>>(invoke, parameter);
    }
}

internal sealed class AsyncStrategy<TContext, TResult> : Strategy<TContext, TResult>
{
    private readonly Func<TContext, CancellationToken, ValueTask<TResult>> _strategy;

    public AsyncStrategy(Func<TContext, CancellationToken, ValueTask<TResult>> strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public override bool CanExecuteSynchronously => false;

    public override bool CanConvertToExpression => false;

    public override TResult Execute(TContext context)
        => throw new NotSupportedException("This strategy only supports asynchronous execution.");

    public override ValueTask<TResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        => _strategy(context, cancellationToken);

    public override Expression<Func<TContext, TResult>> ToExpression()
        => throw new NotSupportedException("This strategy cannot be converted to an expression tree.");

    public override Func<TContext, TResult> Compile()
        => throw new NotSupportedException("This strategy cannot be compiled to a synchronous delegate.");
}
