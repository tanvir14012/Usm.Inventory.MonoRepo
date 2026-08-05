namespace Usm.Shared.Patterns.StateMachine.Abstractions;

/// <summary>
/// Describes a reusable state machine with synchronous and asynchronous firing support.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public interface IStateMachine<TState, TTrigger>
{
    /// <summary>Gets the current state.</summary>
    TState CurrentState { get; }

    /// <summary>Determines whether the supplied trigger can be fired in the current state.</summary>
    bool CanFire(TTrigger trigger);

    /// <summary>Fires the supplied trigger synchronously.</summary>
    TState Fire(TTrigger trigger);

    /// <summary>Fires the supplied trigger asynchronously.</summary>
    ValueTask<TState> FireAsync(TTrigger trigger, CancellationToken cancellationToken = default);
}
