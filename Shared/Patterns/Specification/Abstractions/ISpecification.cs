using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Specification.Abstractions;

/// <summary>
/// Describes a reusable business rule that can be evaluated synchronously, asynchronously, and as an expression tree.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Gets a value indicating whether the specification can be evaluated synchronously.</summary>
    bool CanEvaluateSynchronously { get; }

    /// <summary>Gets a value indicating whether the specification can be evaluated asynchronously.</summary>
    bool CanEvaluateAsynchronously { get; }

    /// <summary>Gets a value indicating whether the specification can be converted to an expression tree.</summary>
    bool CanConvertToExpression { get; }

    /// <summary>Evaluates the specification against the supplied candidate.</summary>
    bool IsSatisfiedBy(T candidate);

    /// <summary>Evaluates the specification against the supplied candidate asynchronously.</summary>
    ValueTask<bool> IsSatisfiedByAsync(T candidate, CancellationToken cancellationToken = default);

    /// <summary>Converts the specification to an expression tree.</summary>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>Compiles the specification to a reusable delegate.</summary>
    Func<T, bool> Compile();
}
