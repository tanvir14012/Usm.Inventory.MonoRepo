using System.Linq.Expressions;
using Usm.Shared.Patterns.Specification.Abstractions;
using Usm.Shared.Patterns.Specification.Extensions;

namespace Usm.Shared.Patterns.Specification.Builders;

/// <summary>
/// Fluent builder for composing reusable specifications.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public sealed class SpecificationBuilder<T>
{
    private readonly ISpecificationFactory<T> _factory;
    private ISpecification<T>? _current;

    /// <summary>
    /// Initializes a new builder.
    /// </summary>
    /// <param name="factory">Optional factory used to create specifications.</param>
    public SpecificationBuilder(ISpecificationFactory<T>? factory = null)
    {
        _factory = factory ?? new SpecificationFactory<T>();
    }

    /// <summary>Adds an expression-based rule to the builder.</summary>
    public SpecificationBuilder<T> Where(Expression<Func<T, bool>> expression)
    {
        return Append(_factory.From(expression), SpecificationCombination.And);
    }

    /// <summary>Adds a synchronous rule to the builder.</summary>
    /// <summary>Adds a synchronous predicate rule to the builder.</summary>
    public SpecificationBuilder<T> WherePredicate(Func<T, bool> predicate)
    {
        return Append(_factory.FromPredicate(predicate), SpecificationCombination.And);
    }

    /// <summary>Adds an asynchronous rule to the builder.</summary>
    public SpecificationBuilder<T> WhereAsync(Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        return Append(_factory.FromAsync(predicate), SpecificationCombination.And);
    }

    /// <summary>Combines the current specification with another one using logical AND.</summary>
    public SpecificationBuilder<T> And(ISpecification<T> specification)
    {
        return Append(specification, SpecificationCombination.And);
    }

    /// <summary>Combines the current specification with another one using logical OR.</summary>
    public SpecificationBuilder<T> Or(ISpecification<T> specification)
    {
        return Append(specification, SpecificationCombination.Or);
    }

    /// <summary>Negates the current specification.</summary>
    public SpecificationBuilder<T> Not()
    {
        _current = _current is null ? _factory.True().Not() : _current.Not();
        return this;
    }

    /// <summary>Builds the composed specification.</summary>
    public ISpecification<T> Build()
    {
        return _current ?? _factory.True();
    }

    private SpecificationBuilder<T> Append(ISpecification<T> specification, SpecificationCombination combination)
    {
        _current = _current is null
            ? specification
            : combination == SpecificationCombination.And
                ? _current.And(specification)
                : _current.Or(specification);

        return this;
    }
}

internal enum SpecificationCombination
{
    And = 0,
    Or = 1
}
