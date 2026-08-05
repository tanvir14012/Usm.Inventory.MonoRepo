namespace Usm.Shared.Patterns.Pipeline.Abstractions;

/// <summary>
/// Compiles expression-backed pipelines and optionally caches the compiled delegates.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IPipelineCompiler<TContext>
{
    /// <summary>Compiles the supplied pipeline to a delegate.</summary>
    Func<TContext, TContext> Compile(IPipeline<TContext> pipeline);
}
