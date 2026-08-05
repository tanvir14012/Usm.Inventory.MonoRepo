using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.Workflow.Abstractions;
using Usm.Shared.Patterns.Workflow.Builders;
using Usm.Shared.Patterns.Workflow.Configuration;

namespace Usm.Shared.Patterns.Workflow.Extensions;

/// <summary>
/// Common extension methods for workflow creation and DI registration.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>Registers the workflow framework with dependency injection.</summary>
    public static IServiceCollection AddWorkflowFramework(
        this IServiceCollection services,
        Action<WorkflowOptions>? configure = null)
    {
        services.AddOptions<WorkflowOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton(typeof(IWorkflowCompiler<>), typeof(WorkflowCompiler<>));
        services.TryAddTransient(typeof(WorkflowBuilder<>), typeof(WorkflowBuilder<>));

        return services;
    }
}

/// <summary>
/// Compiles workflows to reusable synchronous delegates.
/// </summary>
/// <typeparam name="TContext">The workflow context.</typeparam>
public sealed class WorkflowCompiler<TContext> : IWorkflowCompiler<TContext>
{
    private readonly ILogger<WorkflowCompiler<TContext>> _logger;

    /// <summary>Initializes a new compiler.</summary>
    public WorkflowCompiler(ILogger<WorkflowCompiler<TContext>>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowCompiler<TContext>>.Instance;
    }

    /// <inheritdoc />
    public Func<TContext, TContext> Compile(IWorkflow<TContext> workflow)
    {
        if (!workflow.CanExecuteSynchronously)
            throw new NotSupportedException("The workflow cannot be compiled to a synchronous delegate.");

        _logger.LogDebug("Compiling workflow for {Type}.", typeof(TContext).FullName);
        return workflow.Execute;
    }
}
