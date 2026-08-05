using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Specification.Abstractions;
using Usm.Shared.Patterns.Specification.Builders;
using Usm.Shared.Patterns.Specification.Configuration;

namespace Usm.Shared.Patterns.Specification.Extensions;

/// <summary>Common extension methods for specifications and collection filtering.</summary>
public static class SpecificationExtensions
{
    /// <summary>Combines two specifications using logical AND.</summary>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
        => new CompositeSpecification<T>(left, right, SpecificationCombination.And);

    /// <summary>Combines two specifications using logical OR.</summary>
    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
        => new CompositeSpecification<T>(left, right, SpecificationCombination.Or);

    /// <summary>Negates the supplied specification.</summary>
    public static ISpecification<T> Not<T>(this ISpecification<T> specification)
        => new NotSpecification<T>(specification);

    /// <summary>Converts an expression tree to a specification.</summary>
    public static ISpecification<T> ToSpecification<T>(this Expression<Func<T, bool>> expression)
        => Specification<T>.From(expression);

    /// <summary>Converts a synchronous predicate to a specification.</summary>
    public static ISpecification<T> ToSpecificationPredicate<T>(this Func<T, bool> predicate)
        => Specification<T>.FromPredicate(predicate);

    /// <summary>Filters an in-memory sequence using a specification.</summary>
    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> specification)
        => source.Where(specification.IsSatisfiedBy);

    /// <summary>Filters a queryable sequence using a specification expression.</summary>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, ISpecification<T> specification)
        => source.Where(specification.ToExpression());

    /// <summary>Filters an async sequence using a specification.</summary>
    public static async IAsyncEnumerable<T> WhereAsync<T>(
        this IAsyncEnumerable<T> source,
        ISpecification<T> specification,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (await specification.IsSatisfiedByAsync(item, cancellationToken).ConfigureAwait(false))
                yield return item;
        }
    }

    /// <summary>Registers the specification framework with dependency injection.</summary>
    public static IServiceCollection AddSpecificationFramework(
        this IServiceCollection services,
        Action<SpecificationOptions>? configure = null)
    {
        services.AddOptions<SpecificationOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton(typeof(ISpecificationCompiler<>), typeof(SpecificationCompiler<>));
        services.TryAddSingleton(typeof(ISpecificationFactory<>), typeof(SpecificationFactory<>));
        services.TryAddTransient(typeof(SpecificationBuilder<>), typeof(SpecificationBuilder<>));

        return services;
    }
}

/// <summary>
/// Compiles specifications and optionally caches their expression-based delegates.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public sealed class SpecificationCompiler<T> : ISpecificationCompiler<T>
{
    private readonly SpecificationOptions _options;
    private readonly ILogger<SpecificationCompiler<T>> _logger;
    private readonly ConcurrentDictionary<string, Func<T, bool>> _cache = new(StringComparer.Ordinal);

    /// <summary>Initializes a new compiler.</summary>
    public SpecificationCompiler(IOptions<SpecificationOptions> options, ILogger<SpecificationCompiler<T>>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SpecificationCompiler<T>>.Instance;
    }

    /// <inheritdoc />
    public Func<T, bool> Compile(ISpecification<T> specification)
    {
        if (!specification.CanEvaluateSynchronously)
            throw new NotSupportedException("The specification cannot be compiled to a synchronous delegate.");

        if (!_options.CacheCompiledExpressions || !specification.CanConvertToExpression)
            return specification.Compile();

        var expression = specification.ToExpression();
        var cacheKey = expression.ToString();

        return _cache.GetOrAdd(cacheKey, _ =>
        {
            _logger.LogDebug("Caching compiled specification delegate for {SpecificationType}.", typeof(T).FullName);
            return expression.Compile();
        });
    }
}

/// <summary>
/// Creates specifications from expressions and delegates.
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public sealed class SpecificationFactory<T> : ISpecificationFactory<T>
{
    /// <inheritdoc />
    public ISpecification<T> True() => Specification<T>.True();

    /// <inheritdoc />
    public ISpecification<T> False() => Specification<T>.False();

    /// <inheritdoc />
    public ISpecification<T> From(Expression<Func<T, bool>> expression) => Specification<T>.From(expression);

    /// <inheritdoc />
    public ISpecification<T> FromPredicate(Func<T, bool> predicate) => Specification<T>.FromPredicate(predicate);

    /// <inheritdoc />
    public ISpecification<T> FromAsync(Func<T, CancellationToken, ValueTask<bool>> predicate) => Specification<T>.FromAsync(predicate);
}
