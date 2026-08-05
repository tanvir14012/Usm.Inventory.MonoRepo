namespace Usm.Shared.Patterns.RuleEngine.Abstractions;

/// <summary>
/// Compiles rule engines into reusable delegates.
/// </summary>
/// <typeparam name="TContext">The input context.</typeparam>
/// <typeparam name="TResult">The produced result.</typeparam>
public interface IRuleCompiler<TContext, TResult>
{
    /// <summary>Compiles the supplied rule engine to a delegate.</summary>
    Func<TContext, TResult> Compile(IRuleEngine<TContext, TResult> engine, string? group = null);
}
