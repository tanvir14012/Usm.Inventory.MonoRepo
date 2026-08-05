using Usm.Shared.Patterns.StateMachine.Abstractions;
using Usm.Shared.Patterns.StateMachine.Extensions;

namespace Usm.Shared.Patterns.StateMachine;

/// <summary>
/// Default reusable state machine implementation.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed class StateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _states;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _stateLock = new();
    private TState _currentState;

    internal StateMachine(TState initialState, Dictionary<TState, StateConfiguration<TState, TTrigger>> states)
    {
        _currentState = initialState;
        _states = new Dictionary<TState, StateConfiguration<TState, TTrigger>>(states);
    }

    /// <summary>Creates a builder for configuring a state machine.</summary>
    public static Usm.Shared.Patterns.StateMachine.Builders.StateMachineBuilder<TState, TTrigger> CreateBuilder()
        => new();

    /// <inheritdoc />
    public TState CurrentState
    {
        get
        {
            lock (_stateLock)
                return _currentState;
        }
    }

    /// <inheritdoc />
    public bool CanFire(TTrigger trigger)
    {
        lock (_stateLock)
        {
            return CanFireCore(_currentState, trigger);
        }
    }

    /// <inheritdoc />
    public TState Fire(TTrigger trigger)
    {
        _gate.Wait();
        try
        {
            lock (_stateLock)
            {
                return FireCore(_currentState, trigger, CancellationToken.None);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<TState> FireAsync(TTrigger trigger, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_stateLock)
            {
                return FireCore(_currentState, trigger, cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool CanFireCore(TState state, TTrigger trigger)
    {
        if (!_states.TryGetValue(state, out var configuration))
            return false;

        return configuration.Transitions.ContainsKey(trigger) || configuration.IgnoredTriggers.Contains(trigger);
    }

    private TState FireCore(TState state, TTrigger trigger, CancellationToken cancellationToken)
    {
        if (!_states.TryGetValue(state, out var configuration))
        {
            throw new InvalidOperationException($"State '{state}' is not configured.");
        }

        if (configuration.IgnoredTriggers.Contains(trigger))
            return _currentState;

        if (!configuration.Transitions.TryGetValue(trigger, out var transition))
            throw new InvalidOperationException($"Trigger '{trigger}' is not valid for state '{state}'.");

        foreach (var exitAction in configuration.ExitActions)
            exitAction(state, cancellationToken).GetAwaiter().GetResult();

        var nextState = transition.DestinationState;

        if (_states.TryGetValue(nextState, out var nextConfiguration))
        {
            foreach (var entryAction in nextConfiguration.EntryActions)
                entryAction(nextState, cancellationToken).GetAwaiter().GetResult();
        }

        _currentState = nextState;
        return nextState;
    }
}
