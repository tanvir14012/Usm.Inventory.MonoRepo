namespace Usm.Shared.Patterns.StateMachine.Abstractions;

/// <summary>
/// Fluent builder for configuring a reusable state machine.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public interface IStateMachineBuilder<TState, TTrigger>
{
    /// <summary>Configures a state.</summary>
    IStateMachineBuilder<TState, TTrigger> Configure(TState state, Action<IStateConfiguration<TState, TTrigger>> configuration);

    /// <summary>Builds the configured state machine.</summary>
    IStateMachine<TState, TTrigger> Build(TState initialState);
}
