using System.Linq.Expressions;
using Usm.Shared.Patterns.Factory.Abstractions;
using Usm.Shared.Patterns.Factory.Builders;

namespace Usm.Shared.Patterns.Factory;

/// <summary>
/// Base type for reusable factories.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TProduct">The produced type.</typeparam>
public abstract class Factory<TContext, TProduct> : IFactory<TContext, TProduct>
{
    /// <summary>Creates a factory from an expression tree.</summary>
    public static IFactory<TContext, TProduct> From(Expression<Func<TContext, TProduct>> factory)
        => new ExpressionFactory<TContext, TProduct>(factory);

    /// <summary>Creates a factory from a synchronous delegate.</summary>
    public static IFactory<TContext, TProduct> FromPredicate(Func<TContext, TProduct> factory)
        => new PredicateFactory<TContext, TProduct>(factory);

    /// <summary>Creates a factory from an asynchronous delegate.</summary>
    public static IFactory<TContext, TProduct> FromAsync(Func<TContext, CancellationToken, ValueTask<TProduct>> factory)
        => new AsyncFactory<TContext, TProduct>(factory);

    /// <summary>Creates a builder for composing factories.</summary>
    public static FactoryBuilder<TContext, TProduct> CreateBuilder()
        => new();

    /// <inheritdoc />
    public virtual bool CanCreateSynchronously => true;

    /// <inheritdoc />
    public virtual bool CanCreateAsynchronously => true;

    /// <inheritdoc />
    public virtual bool CanConvertToExpression => true;

    /// <inheritdoc />
    public abstract TProduct Create(TContext context);

    /// <inheritdoc />
    public virtual ValueTask<TProduct> CreateAsync(TContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Create(context));

    /// <inheritdoc />
    public abstract Expression<Func<TContext, TProduct>> ToExpression();

    /// <inheritdoc />
    public virtual Func<TContext, TProduct> Compile()
        => ToExpression().Compile();
}

internal sealed class ExpressionFactory<TContext, TProduct> : Factory<TContext, TProduct>
{
    private readonly Expression<Func<TContext, TProduct>> _expression;
    private readonly Lazy<Func<TContext, TProduct>> _compiled;

    public ExpressionFactory(Expression<Func<TContext, TProduct>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _compiled = new Lazy<Func<TContext, TProduct>>(() => _expression.Compile(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override TProduct Create(TContext context)
        => _compiled.Value(context);

    public override Expression<Func<TContext, TProduct>> ToExpression()
        => _expression;

    public override Func<TContext, TProduct> Compile()
        => _compiled.Value;
}

internal sealed class PredicateFactory<TContext, TProduct> : Factory<TContext, TProduct>
{
    private readonly Func<TContext, TProduct> _factory;
    private readonly Lazy<Expression<Func<TContext, TProduct>>> _expression;

    public PredicateFactory(Func<TContext, TProduct> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _expression = new Lazy<Expression<Func<TContext, TProduct>>>(CreateExpression, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override TProduct Create(TContext context)
        => _factory(context);

    public override Expression<Func<TContext, TProduct>> ToExpression()
        => _expression.Value;

    public override Func<TContext, TProduct> Compile()
        => _factory;

    private Expression<Func<TContext, TProduct>> CreateExpression()
    {
        var parameter = Expression.Parameter(typeof(TContext), "context");
        var invoke = Expression.Invoke(Expression.Constant(_factory), parameter);
        return Expression.Lambda<Func<TContext, TProduct>>(invoke, parameter);
    }
}

internal sealed class AsyncFactory<TContext, TProduct> : Factory<TContext, TProduct>
{
    private readonly Func<TContext, CancellationToken, ValueTask<TProduct>> _factory;

    public AsyncFactory(Func<TContext, CancellationToken, ValueTask<TProduct>> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public override bool CanCreateSynchronously => false;

    public override bool CanConvertToExpression => false;

    public override TProduct Create(TContext context)
        => throw new NotSupportedException("This factory only supports asynchronous creation.");

    public override ValueTask<TProduct> CreateAsync(TContext context, CancellationToken cancellationToken = default)
        => _factory(context, cancellationToken);

    public override Expression<Func<TContext, TProduct>> ToExpression()
        => throw new NotSupportedException("This factory cannot be converted to an expression tree.");

    public override Func<TContext, TProduct> Compile()
        => throw new NotSupportedException("This factory cannot be compiled to a synchronous delegate.");
}
