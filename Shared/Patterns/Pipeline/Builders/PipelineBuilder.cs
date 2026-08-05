using System.Linq.Expressions;
using Usm.Shared.Patterns.Pipeline.Abstractions;

namespace Usm.Shared.Patterns.Pipeline.Builders;

/// <summary>
/// Fluent builder for constructing a pipeline.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public sealed class PipelineBuilder<TContext> : IPipelineBuilder<TContext>
{
    private readonly List<Expression<Func<TContext, TContext>>> _steps = [];
    private readonly List<Func<TContext, CancellationToken, ValueTask<TContext>>> _asyncSteps = [];
    private readonly List<Action<TContext>> _finalizers = [];
    private readonly List<Func<TContext, CancellationToken, ValueTask>> _asyncFinalizers = [];

    /// <inheritdoc />
    public IPipelineBuilder<TContext> Use(Expression<Func<TContext, TContext>> step)
    {
        _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
        return this;
    }

    /// <inheritdoc />
    public IPipelineBuilder<TContext> Then(Expression<Func<TContext, TContext>> step)
        => Use(step);

    /// <inheritdoc />
    public IPipelineBuilder<TContext> UseAsync(Func<TContext, CancellationToken, ValueTask<TContext>> step)
    {
        _asyncSteps.Add(step ?? throw new ArgumentNullException(nameof(step)));
        return this;
    }

    /// <inheritdoc />
    public IPipelineBuilder<TContext> ThenAsync(Func<TContext, CancellationToken, ValueTask<TContext>> step)
        => UseAsync(step);

    /// <inheritdoc />
    public IPipelineBuilder<TContext> Finally(Action<TContext> finalizer)
    {
        _finalizers.Add(finalizer ?? throw new ArgumentNullException(nameof(finalizer)));
        return this;
    }

    /// <inheritdoc />
    public IPipelineBuilder<TContext> FinallyAsync(Func<TContext, CancellationToken, ValueTask> finalizer)
    {
        _asyncFinalizers.Add(finalizer ?? throw new ArgumentNullException(nameof(finalizer)));
        return this;
    }

    /// <inheritdoc />
    public IPipeline<TContext> Build()
        => new ExpressionPipeline<TContext>(_steps, _asyncSteps, _finalizers, _asyncFinalizers);
}
