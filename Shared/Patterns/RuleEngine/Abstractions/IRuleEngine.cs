using System.Linq.Expressions;

namespace Usm.Shared.Patterns.RuleEngine.Abstractions;

/// <summary>
/// Describes an ordered rule engine with priority and grouping support.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IRuleEngine<TContext, TResult>
{
    /// <summary>Gets a value indicating whether the engine can be executed synchronously.</summary>
    bool CanExecuteSynchronously { get; }

    /// <summary>Gets a value indicating whether the engine can be converted to an expression tree.</summary>
    bool CanConvertToExpression { get; }

    /// <summary>Evaluates the engine for the supplied context.</summary>
    TResult Evaluate(TContext context, string? group = null);

    /// <summary>Evaluates the engine asynchronously for the supplied context.</summary>
    ValueTask<TResult> EvaluateAsync(TContext context, string? group = null, CancellationToken cancellationToken = default);

    /// <summary>Converts the engine to an expression tree when possible.</summary>
    Expression<Func<TContext, TResult>> ToExpression(string? group = null);

    /// <summary>Compiles the engine to a reusable delegate.</summary>
    Func<TContext, TResult> Compile(string? group = null);
}
