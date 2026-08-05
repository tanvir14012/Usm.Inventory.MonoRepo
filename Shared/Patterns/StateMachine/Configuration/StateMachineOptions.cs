namespace Usm.Shared.Patterns.StateMachine.Configuration;

/// <summary>
/// Configuration for the state machine framework.
/// </summary>
public sealed class StateMachineOptions
{
    /// <summary>Gets or sets a value indicating whether unknown triggers should throw immediately.</summary>
    public bool ThrowOnUnknownTrigger { get; set; } = true;
}
