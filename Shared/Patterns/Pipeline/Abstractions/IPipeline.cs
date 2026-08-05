using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Pipeline.Abstractions;

/// <summary>
/// Describes a reusable pipeline that transforms a context through ordered steps.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IPipeline<TContext>
{
    /// <summary>Gets a value indicating whether synchronous execution is supported.</summary>
    bool CanExecuteSynchronously { get; }

    /// <summary>Gets a value indicating whether asynchronous execution is supported.</summary>
    bool CanExecuteAsynchronously { get; }

    /// <summary>Gets a value indicating whether the pipeline can be converted to an expression tree.</summary>
    bool CanConvertToExpression { get; }

    /// <summary>Executes the pipeline synchronously.</summary>
    TContext Execute(TContext context);

    /// <summary>Executes the pipeline asynchronously.</summary>
    ValueTask<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    /// <summary>Converts the pipeline to an expression tree when possible.</summary>
    Expression<Func<TContext, TContext>> ToExpression();

    /// <summary>Compiles the pipeline to a reusable delegate.</summary>
    Func<TContext, TContext> Compile();
}
