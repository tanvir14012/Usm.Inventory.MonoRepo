namespace Usm.Shared.Patterns.Strategy.Abstractions;

/// <summary>
/// Compiles expression-backed strategies and optionally caches the compiled delegates.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IStrategyCompiler<TContext, TResult>
{
    /// <summary>Compiles the supplied strategy to a delegate.</summary>
    Func<TContext, TResult> Compile(IStrategy<TContext, TResult> strategy);
}
