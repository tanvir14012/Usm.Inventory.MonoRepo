using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Usm.Shared.Patterns.StateMachine.Abstractions;
using Usm.Shared.Patterns.StateMachine.Builders;
using Usm.Shared.Patterns.StateMachine.Configuration;

namespace Usm.Shared.Patterns.StateMachine.Extensions;

/// <summary>
/// Common extension methods for state machine creation and DI registration.
/// </summary>
public static class StateMachineExtensions
{
    /// <summary>Registers the state machine framework with dependency injection.</summary>
    public static IServiceCollection AddStateMachineFramework(
        this IServiceCollection services,
        Action<StateMachineOptions>? configure = null)
    {
        services.AddOptions<StateMachineOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddTransient(typeof(IStateMachineBuilder<,>), typeof(StateMachineBuilder<,>));

        return services;
    }
}

/// <summary>
/// Builds state configurations and validates transitions.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
internal sealed class StateConfiguration<TState, TTrigger> : IStateConfiguration<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly Dictionary<TTrigger, StateTransition<TState>> _transitions = new();
    private readonly HashSet<TTrigger> _ignored = new();
    private readonly List<Func<TState, CancellationToken, ValueTask>> _entryActions = new();
    private readonly List<Func<TState, CancellationToken, ValueTask>> _exitActions = new();

    public IReadOnlyDictionary<TTrigger, StateTransition<TState>> Transitions => _transitions;
    public IReadOnlyCollection<TTrigger> IgnoredTriggers => _ignored;
    public IReadOnlyList<Func<TState, CancellationToken, ValueTask>> EntryActions => _entryActions;
    public IReadOnlyList<Func<TState, CancellationToken, ValueTask>> ExitActions => _exitActions;

    public IStateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState destinationState)
    {
        _transitions[trigger] = StateTransition<TState>.Permit(destinationState);
        _ignored.Remove(trigger);
        return this;
    }

    public IStateConfiguration<TState, TTrigger> Ignore(TTrigger trigger)
    {
        _ignored.Add(trigger);
        _transitions.Remove(trigger);
        return this;
    }

    public IStateConfiguration<TState, TTrigger> OnEntry(Action<TState> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _entryActions.Add((state, _) => { action(state); return ValueTask.CompletedTask; });
        return this;
    }

    public IStateConfiguration<TState, TTrigger> OnEntry(Func<TState, CancellationToken, ValueTask> action)
    {
        _entryActions.Add(action ?? throw new ArgumentNullException(nameof(action)));
        return this;
    }

    public IStateConfiguration<TState, TTrigger> OnExit(Action<TState> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _exitActions.Add((state, _) => { action(state); return ValueTask.CompletedTask; });
        return this;
    }

    public IStateConfiguration<TState, TTrigger> OnExit(Func<TState, CancellationToken, ValueTask> action)
    {
        _exitActions.Add(action ?? throw new ArgumentNullException(nameof(action)));
        return this;
    }
}

/// <summary>
/// Describes a permitted transition.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
internal readonly record struct StateTransition<TState>(TState DestinationState)
{
    public static StateTransition<TState> Permit(TState destinationState) => new(destinationState);
}
