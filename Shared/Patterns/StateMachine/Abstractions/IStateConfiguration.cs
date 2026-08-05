namespace Usm.Shared.Patterns.StateMachine.Abstractions;

/// <summary>
/// Configures the permitted transitions and lifecycle callbacks for a state.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public interface IStateConfiguration<TState, TTrigger>
{
    /// <summary>Permits a trigger and transitions to the destination state.</summary>
    IStateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState destinationState);

    /// <summary>Ignores a trigger in the current state.</summary>
    IStateConfiguration<TState, TTrigger> Ignore(TTrigger trigger);

    /// <summary>Adds a synchronous entry action.</summary>
    IStateConfiguration<TState, TTrigger> OnEntry(Action<TState> action);

    /// <summary>Adds an asynchronous entry action.</summary>
    IStateConfiguration<TState, TTrigger> OnEntry(Func<TState, CancellationToken, ValueTask> action);

    /// <summary>Adds a synchronous exit action.</summary>
    IStateConfiguration<TState, TTrigger> OnExit(Action<TState> action);

    /// <summary>Adds an asynchronous exit action.</summary>
    IStateConfiguration<TState, TTrigger> OnExit(Func<TState, CancellationToken, ValueTask> action);
}
