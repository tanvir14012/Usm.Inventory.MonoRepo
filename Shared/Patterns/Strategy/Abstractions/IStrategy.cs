using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Strategy.Abstractions;

/// <summary>
/// Describes a reusable algorithm that can be executed synchronously, asynchronously, and as an expression tree.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IStrategy<TContext, TResult>
{
    /// <summary>Gets a value indicating whether synchronous execution is supported.</summary>
    bool CanExecuteSynchronously { get; }

    /// <summary>Gets a value indicating whether asynchronous execution is supported.</summary>
    bool CanExecuteAsynchronously { get; }

    /// <summary>Gets a value indicating whether the strategy can be converted to an expression tree.</summary>
    bool CanConvertToExpression { get; }

    /// <summary>Executes the strategy for the supplied context.</summary>
    TResult Execute(TContext context);

    /// <summary>Executes the strategy asynchronously for the supplied context.</summary>
    ValueTask<TResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    /// <summary>Converts the strategy to an expression tree when possible.</summary>
    Expression<Func<TContext, TResult>> ToExpression();

    /// <summary>Compiles the strategy to a reusable delegate.</summary>
    Func<TContext, TResult> Compile();
}
