using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Specification.Abstractions;

/// <summary>
/// Creates specifications from expressions, delegates, and asynchronous predicates.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public interface ISpecificationFactory<T>
{
    /// <summary>Creates a specification that always evaluates to true.</summary>
    ISpecification<T> True();

    /// <summary>Creates a specification that always evaluates to false.</summary>
    ISpecification<T> False();

    /// <summary>Creates a specification from an expression tree.</summary>
    ISpecification<T> From(Expression<Func<T, bool>> expression);

    /// <summary>Creates a specification from a synchronous predicate.</summary>
    ISpecification<T> FromPredicate(Func<T, bool> predicate);

    /// <summary>Creates a specification from an asynchronous predicate.</summary>
    ISpecification<T> FromAsync(Func<T, CancellationToken, ValueTask<bool>> predicate);
}
