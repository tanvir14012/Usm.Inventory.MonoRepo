using System.Linq.Expressions;

namespace Usm.Shared.Patterns.RuleEngine;

internal sealed record RuleDefinition<TContext, TResult>
{
    private RuleDefinition(
        int priority,
        string? group,
        long sequence,
        Func<TContext, bool> predicate,
        Func<TContext, CancellationToken, ValueTask<bool>> predicateAsync,
        Func<TContext, TResult> result,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultAsync,
        bool canExecuteSynchronously,
        bool canConvertToExpression)
    {
        Priority = priority;
        Group = group;
        Sequence = sequence;
        Predicate = predicate;
        PredicateAsync = predicateAsync;
        Result = result;
        ResultAsync = resultAsync;
        CanExecuteSynchronously = canExecuteSynchronously;
        CanConvertToExpression = canConvertToExpression;
    }

    public int Priority { get; }
    public string? Group { get; }
    public long Sequence { get; }
    public Func<TContext, bool> Predicate { get; }
    public Func<TContext, CancellationToken, ValueTask<bool>> PredicateAsync { get; }
    public Func<TContext, TResult> Result { get; }
    public Func<TContext, CancellationToken, ValueTask<TResult>> ResultAsync { get; }
    public bool CanExecuteSynchronously { get; }
    public bool CanConvertToExpression { get; }

    public static RuleDefinition<TContext, TResult> FromExpression(
        Expression<Func<TContext, bool>> predicate,
        Expression<Func<TContext, TResult>> result,
        int priority,
        string? group,
        long sequence)
    {
        var compiledPredicate = predicate.Compile();
        var compiledResult = result.Compile();
        return new RuleDefinition<TContext, TResult>(
            priority,
            group,
            sequence,
            compiledPredicate,
            static (context, _) => ValueTask.FromResult(false),
            compiledResult,
            static (context, _) => throw new NotSupportedException(),
            true,
            true);
    }

    public static RuleDefinition<TContext, TResult> FromPredicate(
        Func<TContext, bool> predicate,
        Func<TContext, TResult> result,
        int priority,
        string? group,
        long sequence)
    {
        return new RuleDefinition<TContext, TResult>(
            priority,
            group,
            sequence,
            predicate ?? throw new ArgumentNullException(nameof(predicate)),
            static (context, _) => ValueTask.FromResult(false),
            result ?? throw new ArgumentNullException(nameof(result)),
            static (context, _) => throw new NotSupportedException(),
            true,
            true);
    }

    public static RuleDefinition<TContext, TResult> FromAsync(
        Func<TContext, CancellationToken, ValueTask<bool>> predicate,
        Func<TContext, CancellationToken, ValueTask<TResult>> result,
        int priority,
        string? group,
        long sequence)
    {
        return new RuleDefinition<TContext, TResult>(
            priority,
            group,
            sequence,
            static context => throw new NotSupportedException("This rule requires asynchronous execution."),
            predicate ?? throw new ArgumentNullException(nameof(predicate)),
            static context => throw new NotSupportedException("This rule requires asynchronous execution."),
            result ?? throw new ArgumentNullException(nameof(result)),
            false,
            false);
    }
}

internal sealed record FallbackDefinition<TContext, TResult>
{
    private FallbackDefinition(
        string? group,
        Func<TContext, TResult> result,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultAsync,
        bool canExecuteSynchronously,
        bool canConvertToExpression)
    {
        Group = group;
        Result = result;
        ResultAsync = resultAsync;
        CanExecuteSynchronously = canExecuteSynchronously;
        CanConvertToExpression = canConvertToExpression;
    }

    public string? Group { get; }
    public Func<TContext, TResult> Result { get; }
    public Func<TContext, CancellationToken, ValueTask<TResult>> ResultAsync { get; }
    public bool CanExecuteSynchronously { get; }
    public bool CanConvertToExpression { get; }

    public static FallbackDefinition<TContext, TResult> FromExpression(Expression<Func<TContext, TResult>> result, string? group)
        => new(group, result.Compile(), static (_, _) => throw new NotSupportedException(), true, true);

    public static FallbackDefinition<TContext, TResult> FromPredicate(Func<TContext, TResult> result, string? group)
        => new(group, result ?? throw new ArgumentNullException(nameof(result)), static (_, _) => throw new NotSupportedException(), true, true);

    public static FallbackDefinition<TContext, TResult> FromAsync(Func<TContext, CancellationToken, ValueTask<TResult>> result, string? group)
        => new(group, static _ => throw new NotSupportedException("This fallback requires asynchronous execution."), result ?? throw new ArgumentNullException(nameof(result)), false, false);
}
