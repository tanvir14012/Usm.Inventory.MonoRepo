using Usm.Shared.Patterns.StateMachine.Abstractions;
using Usm.Shared.Patterns.StateMachine.Extensions;
using Usm.Shared.Patterns.StateMachine;

namespace Usm.Shared.Patterns.StateMachine.Builders;

/// <summary>
/// Fluent builder for a reusable state machine.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed class StateMachineBuilder<TState, TTrigger> : IStateMachineBuilder<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _states = new();

    /// <inheritdoc />
    public IStateMachineBuilder<TState, TTrigger> Configure(TState state, Action<IStateConfiguration<TState, TTrigger>> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var stateConfiguration = GetOrCreateConfiguration(state);
        configuration(stateConfiguration);
        return this;
    }

    /// <inheritdoc />
    public IStateMachine<TState, TTrigger> Build(TState initialState)
        => new StateMachine<TState, TTrigger>(initialState, _states);

    internal StateConfiguration<TState, TTrigger> GetOrCreateConfiguration(TState state)
    {
        if (_states.TryGetValue(state, out var configuration))
            return configuration;

        configuration = new StateConfiguration<TState, TTrigger>();
        _states[state] = configuration;
        return configuration;
    }
}
