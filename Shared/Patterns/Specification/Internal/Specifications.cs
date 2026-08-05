using System.Linq.Expressions;
using Usm.Shared.Patterns.Specification.Abstractions;
using Usm.Shared.Patterns.Specification.Builders;
using Usm.Shared.Patterns.Specification.Internal;

namespace Usm.Shared.Patterns.Specification;

/// <summary>
/// Base type for reusable specifications.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public virtual bool CanEvaluateSynchronously => true;

    /// <inheritdoc />
    public virtual bool CanEvaluateAsynchronously => true;

    /// <inheritdoc />
    public virtual bool CanConvertToExpression => true;

    /// <summary>Creates a specification from an expression tree.</summary>
    public static ISpecification<T> From(Expression<Func<T, bool>> expression)
        => new ExpressionSpecification<T>(expression);

    /// <summary>Creates a specification from a synchronous predicate.</summary>
    public static ISpecification<T> FromPredicate(Func<T, bool> predicate)
        => new PredicateSpecification<T>(predicate);

    /// <summary>Creates a specification from an asynchronous predicate.</summary>
    public static ISpecification<T> FromAsync(Func<T, CancellationToken, ValueTask<bool>> predicate)
        => new AsyncSpecification<T>(predicate);

    /// <summary>Creates a specification that always evaluates to true.</summary>
    public static ISpecification<T> True()
        => new PredicateSpecification<T>(static _ => true);

    /// <summary>Creates a specification that always evaluates to false.</summary>
    public static ISpecification<T> False()
        => new PredicateSpecification<T>(static _ => false);

    /// <summary>Creates a fluent builder for composing specifications.</summary>
    public static SpecificationBuilder<T> CreateBuilder()
        => new SpecificationBuilder<T>();

    /// <inheritdoc />
    public abstract bool IsSatisfiedBy(T candidate);

    /// <inheritdoc />
    public virtual ValueTask<bool> IsSatisfiedByAsync(T candidate, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(IsSatisfiedBy(candidate));

    /// <inheritdoc />
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <inheritdoc />
    public virtual Func<T, bool> Compile()
        => ToExpression().Compile();

    /// <summary>Combines the specification with another specification using AND.</summary>
    public ISpecification<T> And(ISpecification<T> other)
        => new CompositeSpecification<T>(this, other, SpecificationCombination.And);

    /// <summary>Combines the specification with another specification using OR.</summary>
    public ISpecification<T> Or(ISpecification<T> other)
        => new CompositeSpecification<T>(this, other, SpecificationCombination.Or);

    /// <summary>Negates the specification.</summary>
    public ISpecification<T> Not()
        => new NotSpecification<T>(this);
}

internal sealed class ExpressionSpecification<T> : Specification<T>
{
    private readonly Expression<Func<T, bool>> _expression;
    private readonly Lazy<Func<T, bool>> _compiled;

    public ExpressionSpecification(Expression<Func<T, bool>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _compiled = new Lazy<Func<T, bool>>(() => _expression.Compile(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override bool IsSatisfiedBy(T candidate)
        => _compiled.Value(candidate);

    public override Expression<Func<T, bool>> ToExpression()
        => _expression;

    public override Func<T, bool> Compile()
        => _compiled.Value;
}

internal sealed class PredicateSpecification<T> : Specification<T>
{
    private readonly Func<T, bool> _predicate;
    private readonly Lazy<Expression<Func<T, bool>>> _expression;

    public PredicateSpecification(Func<T, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _expression = new Lazy<Expression<Func<T, bool>>>(CreateExpression, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override bool IsSatisfiedBy(T candidate)
        => _predicate(candidate);

    public override Expression<Func<T, bool>> ToExpression()
        => _expression.Value;

    public override Func<T, bool> Compile()
        => _predicate;

    private Expression<Func<T, bool>> CreateExpression()
    {
        var parameter = Expression.Parameter(typeof(T), "candidate");
        var invoke = Expression.Invoke(Expression.Constant(_predicate), parameter);
        return Expression.Lambda<Func<T, bool>>(invoke, parameter);
    }
}

internal sealed class AsyncSpecification<T> : Specification<T>
{
    private readonly Func<T, CancellationToken, ValueTask<bool>> _predicate;

    public AsyncSpecification(Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public override bool CanEvaluateSynchronously => false;

    public override bool CanConvertToExpression => false;

    public override bool IsSatisfiedBy(T candidate)
        => throw new NotSupportedException("This specification only supports asynchronous evaluation.");

    public override ValueTask<bool> IsSatisfiedByAsync(T candidate, CancellationToken cancellationToken = default)
        => _predicate(candidate, cancellationToken);

    public override Expression<Func<T, bool>> ToExpression()
        => throw new NotSupportedException("This specification cannot be converted to an expression tree.");

    public override Func<T, bool> Compile()
        => throw new NotSupportedException("This specification cannot be compiled to a synchronous delegate.");
}

internal sealed class CompositeSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;
    private readonly SpecificationCombination _combination;
    private readonly Lazy<Func<T, bool>>? _compiled;
    private readonly Lazy<Expression<Func<T, bool>>>? _expression;

    public CompositeSpecification(ISpecification<T> left, ISpecification<T> right, SpecificationCombination combination)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _combination = combination;

        if (CanEvaluateSynchronously)
            _compiled = new Lazy<Func<T, bool>>(CreateCompiled, LazyThreadSafetyMode.ExecutionAndPublication);

        if (CanConvertToExpression)
            _expression = new Lazy<Expression<Func<T, bool>>>(CreateExpression, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override bool CanEvaluateSynchronously
        => _left.CanEvaluateSynchronously && _right.CanEvaluateSynchronously;

    public override bool CanEvaluateAsynchronously
        => _left.CanEvaluateAsynchronously && _right.CanEvaluateAsynchronously;

    public override bool CanConvertToExpression
        => _left.CanConvertToExpression && _right.CanConvertToExpression;

    public override bool IsSatisfiedBy(T candidate)
    {
        if (_compiled is null)
            throw new NotSupportedException("This specification cannot be evaluated synchronously.");

        return _compiled.Value(candidate);
    }

    public override async ValueTask<bool> IsSatisfiedByAsync(T candidate, CancellationToken cancellationToken = default)
    {
        if (!CanEvaluateAsynchronously)
            throw new NotSupportedException("This specification cannot be evaluated asynchronously.");

        var left = await _left.IsSatisfiedByAsync(candidate, cancellationToken).ConfigureAwait(false);
        if (_combination == SpecificationCombination.And && !left)
            return false;
        if (_combination == SpecificationCombination.Or && left)
            return true;

        var right = await _right.IsSatisfiedByAsync(candidate, cancellationToken).ConfigureAwait(false);
        return _combination == SpecificationCombination.And ? left && right : left || right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        if (_expression is null)
            throw new NotSupportedException("This specification cannot be converted to an expression tree.");

        return _expression.Value;
    }

    public override Func<T, bool> Compile()
    {
        if (_compiled is null)
            throw new NotSupportedException("This specification cannot be compiled to a synchronous delegate.");

        return _compiled.Value;
    }

    private Func<T, bool> CreateCompiled()
        => _combination == SpecificationCombination.And
            ? candidate => _left.IsSatisfiedBy(candidate) && _right.IsSatisfiedBy(candidate)
            : candidate => _left.IsSatisfiedBy(candidate) || _right.IsSatisfiedBy(candidate);

    private Expression<Func<T, bool>> CreateExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        return _combination == SpecificationCombination.And
            ? ExpressionComposer.And(leftExpression, rightExpression)
            : ExpressionComposer.Or(leftExpression, rightExpression);
    }
}

internal sealed class NotSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _inner;
    private readonly Lazy<Func<T, bool>>? _compiled;
    private readonly Lazy<Expression<Func<T, bool>>>? _expression;

    public NotSpecification(ISpecification<T> inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        if (CanEvaluateSynchronously)
            _compiled = new Lazy<Func<T, bool>>(() => candidate => !_inner.IsSatisfiedBy(candidate), LazyThreadSafetyMode.ExecutionAndPublication);

        if (CanConvertToExpression)
            _expression = new Lazy<Expression<Func<T, bool>>>(() => ExpressionComposer.Not(_inner.ToExpression()), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override bool CanEvaluateSynchronously => _inner.CanEvaluateSynchronously;

    public override bool CanEvaluateAsynchronously => _inner.CanEvaluateAsynchronously;

    public override bool CanConvertToExpression => _inner.CanConvertToExpression;

    public override bool IsSatisfiedBy(T candidate)
    {
        if (_compiled is null)
            throw new NotSupportedException("This specification cannot be evaluated synchronously.");

        return _compiled.Value(candidate);
    }

    public override async ValueTask<bool> IsSatisfiedByAsync(T candidate, CancellationToken cancellationToken = default)
    {
        if (!CanEvaluateAsynchronously)
            throw new NotSupportedException("This specification cannot be evaluated asynchronously.");

        return !await _inner.IsSatisfiedByAsync(candidate, cancellationToken).ConfigureAwait(false);
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        if (_expression is null)
            throw new NotSupportedException("This specification cannot be converted to an expression tree.");

        return _expression.Value;
    }

    public override Func<T, bool> Compile()
    {
        if (_compiled is null)
            throw new NotSupportedException("This specification cannot be compiled to a synchronous delegate.");

        return _compiled.Value;
    }
}
