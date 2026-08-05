using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Factory.Abstractions;

/// <summary>
/// Creates products from a context in a sync, async, and expression-friendly way.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TProduct">The produced type.</typeparam>
public interface IFactory<TContext, TProduct>
{
    /// <summary>Gets a value indicating whether synchronous creation is supported.</summary>
    bool CanCreateSynchronously { get; }

    /// <summary>Gets a value indicating whether asynchronous creation is supported.</summary>
    bool CanCreateAsynchronously { get; }

    /// <summary>Gets a value indicating whether the factory can be converted to an expression tree.</summary>
    bool CanConvertToExpression { get; }

    /// <summary>Creates a product from the supplied context.</summary>
    TProduct Create(TContext context);

    /// <summary>Creates a product from the supplied context asynchronously.</summary>
    ValueTask<TProduct> CreateAsync(TContext context, CancellationToken cancellationToken = default);

    /// <summary>Converts the factory to an expression tree when possible.</summary>
    Expression<Func<TContext, TProduct>> ToExpression();

    /// <summary>Compiles the factory to a reusable delegate.</summary>
    Func<TContext, TProduct> Compile();
}
