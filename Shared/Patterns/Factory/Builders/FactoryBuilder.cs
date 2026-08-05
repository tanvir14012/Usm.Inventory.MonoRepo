using System.Linq.Expressions;
using Usm.Shared.Patterns.Factory.Abstractions;

namespace Usm.Shared.Patterns.Factory.Builders;

/// <summary>
/// Fluent builder for constructing a factory.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TProduct">The produced type.</typeparam>
public sealed class FactoryBuilder<TContext, TProduct> : IFactoryBuilder<TContext, TProduct>
{
    private IFactory<TContext, TProduct>? _current;

    /// <inheritdoc />
    public IFactoryBuilder<TContext, TProduct> UseExpression(Expression<Func<TContext, TProduct>> factory)
    {
        _current = Factory<TContext, TProduct>.From(factory);
        return this;
    }

    /// <inheritdoc />
    public IFactoryBuilder<TContext, TProduct> UsePredicate(Func<TContext, TProduct> factory)
    {
        _current = Factory<TContext, TProduct>.FromPredicate(factory);
        return this;
    }

    /// <inheritdoc />
    public IFactoryBuilder<TContext, TProduct> UseAsync(Func<TContext, CancellationToken, ValueTask<TProduct>> factory)
    {
        _current = Factory<TContext, TProduct>.FromAsync(factory);
        return this;
    }

    /// <inheritdoc />
    public IFactory<TContext, TProduct> Build()
        => _current ?? throw new InvalidOperationException("No factory has been configured.");
}
