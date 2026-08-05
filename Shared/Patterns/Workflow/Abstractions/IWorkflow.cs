namespace Usm.Shared.Patterns.Workflow.Abstractions;

/// <summary>
/// Describes a reusable workflow that executes a sequence of steps.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public interface IWorkflow<TContext>
{
    /// <summary>Gets a value indicating whether the workflow can be executed synchronously.</summary>
    bool CanExecuteSynchronously { get; }

    /// <summary>Executes the workflow synchronously.</summary>
    TContext Execute(TContext context);

    /// <summary>Executes the workflow asynchronously.</summary>
    ValueTask<TContext> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}
