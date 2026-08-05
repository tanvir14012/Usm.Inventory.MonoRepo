using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Pipeline.Abstractions;

/// <summary>
/// Fluent builder for constructing a reusable pipeline.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IPipelineBuilder<TContext>
{
    /// <summary>Adds a synchronous step.</summary>
    IPipelineBuilder<TContext> Use(Expression<Func<TContext, TContext>> step);

    /// <summary>Adds another synchronous step.</summary>
    IPipelineBuilder<TContext> Then(Expression<Func<TContext, TContext>> step);

    /// <summary>Adds an asynchronous step.</summary>
    IPipelineBuilder<TContext> UseAsync(Func<TContext, CancellationToken, ValueTask<TContext>> step);

    /// <summary>Adds another asynchronous step.</summary>
    IPipelineBuilder<TContext> ThenAsync(Func<TContext, CancellationToken, ValueTask<TContext>> step);

    /// <summary>Adds a finalizer that runs after the main pipeline completes.</summary>
    IPipelineBuilder<TContext> Finally(Action<TContext> finalizer);

    /// <summary>Adds an asynchronous finalizer that runs after the main pipeline completes.</summary>
    IPipelineBuilder<TContext> FinallyAsync(Func<TContext, CancellationToken, ValueTask> finalizer);

    /// <summary>Builds the configured pipeline.</summary>
    IPipeline<TContext> Build();
}
