namespace Usm.Shared.Patterns.Workflow.Abstractions;

/// <summary>
/// Compiles workflows to reusable delegates.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public interface IWorkflowCompiler<TContext>
{
    /// <summary>Compiles the supplied workflow to a synchronous delegate.</summary>
    Func<TContext, TContext> Compile(IWorkflow<TContext> workflow);
}
