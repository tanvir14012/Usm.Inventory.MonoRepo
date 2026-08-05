using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Factory.Abstractions;

/// <summary>
/// Fluent builder for composing reusable factories.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TProduct">The produced type.</typeparam>
public interface IFactoryBuilder<TContext, TProduct>
{
    /// <summary>Adds an expression-based factory.</summary>
    IFactoryBuilder<TContext, TProduct> UseExpression(Expression<Func<TContext, TProduct>> factory);

    /// <summary>Adds a synchronous factory delegate.</summary>
    IFactoryBuilder<TContext, TProduct> UsePredicate(Func<TContext, TProduct> factory);

    /// <summary>Adds an asynchronous factory delegate.</summary>
    IFactoryBuilder<TContext, TProduct> UseAsync(Func<TContext, CancellationToken, ValueTask<TProduct>> factory);

    /// <summary>Builds the configured factory.</summary>
    IFactory<TContext, TProduct> Build();
}
