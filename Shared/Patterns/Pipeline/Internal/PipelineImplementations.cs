using System.Linq.Expressions;
using Usm.Shared.Patterns.Pipeline.Abstractions;
using Usm.Shared.Patterns.Pipeline.Builders;

namespace Usm.Shared.Patterns.Pipeline;

/// <summary>
/// Base type for reusable pipelines.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public abstract class Pipeline<TContext> : IPipeline<TContext>
{
    /// <summary>Creates a pipeline from a synchronous step.</summary>
    public static IPipeline<TContext> From(Expression<Func<TContext, TContext>> step)
        => new ExpressionPipeline<TContext>([step], [], [], []);

    /// <summary>Creates a builder for composing a pipeline.</summary>
    public static PipelineBuilder<TContext> CreateBuilder()
        => new();

    /// <inheritdoc />
    public virtual bool CanExecuteSynchronously => true;

    /// <inheritdoc />
    public virtual bool CanExecuteAsynchronously => true;

    /// <inheritdoc />
    public virtual bool CanConvertToExpression => true;

    /// <inheritdoc />
    public abstract TContext Execute(TContext context);

    /// <inheritdoc />
    public virtual ValueTask<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Execute(context));

    /// <inheritdoc />
    public abstract Expression<Func<TContext, TContext>> ToExpression();

    /// <inheritdoc />
    public virtual Func<TContext, TContext> Compile()
        => ToExpression().Compile();
}

internal sealed class ExpressionPipeline<TContext> : Pipeline<TContext>
{
    private readonly IReadOnlyList<Expression<Func<TContext, TContext>>> _steps;
    private readonly Func<TContext, TContext>[] _compiledSteps;
    private readonly IReadOnlyList<Func<TContext, CancellationToken, ValueTask<TContext>>> _asyncSteps;
    private readonly IReadOnlyList<Action<TContext>> _finalizers;
    private readonly IReadOnlyList<Func<TContext, CancellationToken, ValueTask>> _asyncFinalizers;
    private readonly Lazy<Func<TContext, TContext>> _compiled;
    private readonly Lazy<Expression<Func<TContext, TContext>>> _expression;

    public ExpressionPipeline(
        IReadOnlyList<Expression<Func<TContext, TContext>>> steps,
        IReadOnlyList<Func<TContext, CancellationToken, ValueTask<TContext>>> asyncSteps,
        IReadOnlyList<Action<TContext>> finalizers,
        IReadOnlyList<Func<TContext, CancellationToken, ValueTask>> asyncFinalizers)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _compiledSteps = _steps.Select(step => step.Compile()).ToArray();
        _asyncSteps = asyncSteps ?? throw new ArgumentNullException(nameof(asyncSteps));
        _finalizers = finalizers ?? throw new ArgumentNullException(nameof(finalizers));
        _asyncFinalizers = asyncFinalizers ?? throw new ArgumentNullException(nameof(asyncFinalizers));
        _compiled = new Lazy<Func<TContext, TContext>>(() => BuildCompiled(), LazyThreadSafetyMode.ExecutionAndPublication);
        _expression = new Lazy<Expression<Func<TContext, TContext>>>(BuildExpression, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override bool CanExecuteSynchronously => _asyncSteps.Count == 0 && _asyncFinalizers.Count == 0;

    public override bool CanConvertToExpression => CanExecuteSynchronously && _finalizers.Count == 0;

    public override TContext Execute(TContext context)
    {
        if (!CanExecuteSynchronously)
            throw new NotSupportedException("This pipeline requires asynchronous execution.");

        return _compiled.Value(context);
    }

    public override async ValueTask<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var current = context;

        foreach (var step in _compiledSteps)
            current = step(current);

        foreach (var step in _asyncSteps)
            current = await step(current, cancellationToken).ConfigureAwait(false);

        foreach (var finalizer in _finalizers)
            finalizer(current);

        foreach (var finalizer in _asyncFinalizers)
            await finalizer(current, cancellationToken).ConfigureAwait(false);

        return current;
    }

    public override Expression<Func<TContext, TContext>> ToExpression()
    {
        if (!CanConvertToExpression)
            throw new NotSupportedException("This pipeline cannot be converted to an expression tree.");

        return _expression.Value;
    }

    public override Func<TContext, TContext> Compile()
    {
        if (!CanExecuteSynchronously)
            throw new NotSupportedException("This pipeline cannot be compiled to a synchronous delegate.");

        return _compiled.Value;
    }

    private Func<TContext, TContext> BuildCompiled()
    {
        return context =>
        {
            var current = context;
            foreach (var step in _compiledSteps)
                current = step(current);

            foreach (var finalizer in _finalizers)
                finalizer(current);

            return current;
        };
    }

    private Expression<Func<TContext, TContext>> BuildExpression()
    {
        if (_steps.Count == 0)
        {
            var identityParameter = Expression.Parameter(typeof(TContext), "context");
            return Expression.Lambda<Func<TContext, TContext>>(identityParameter, identityParameter);
        }

        var parameter = Expression.Parameter(typeof(TContext), "context");
        Expression body = parameter;

        foreach (var step in _steps)
        {
            body = new ExpressionSubstituter(step.Parameters[0], body).Visit(step.Body)
                ?? throw new InvalidOperationException("Failed to rewrite pipeline expression.");
        }

        return Expression.Lambda<Func<TContext, TContext>>(body, parameter);
    }
}

internal sealed class ExpressionSubstituter : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly Expression _target;

    public ExpressionSubstituter(ParameterExpression source, Expression target)
    {
        _source = source;
        _target = target;
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _source ? _target : base.VisitParameter(node);
}
