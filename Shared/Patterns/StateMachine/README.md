# StateMachine<TState, TTrigger>

Reusable state machine abstraction for explicit transitions, ignored triggers, entry/exit actions, and async firing.

## Folder structure

```text
Shared/Patterns/StateMachine
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
└── README.md
```

## Capabilities

- `Configure`
- `Permit`
- `Ignore`
- `OnEntry`
- `OnExit`
- `CanFire`
- `Fire` and `FireAsync`
- DI registration via `AddStateMachineFramework`

## Example

```csharp
var machine = StateMachine<OrderState, OrderTrigger>.CreateBuilder()
    .Configure(OrderState.Draft, state => state
        .Permit(OrderTrigger.Submit, OrderState.Submitted)
        .OnExit(s => logger.LogInformation("Leaving {State}", s)))
    .Configure(OrderState.Submitted, state => state
        .Ignore(OrderTrigger.Submit))
    .Build(OrderState.Draft);

await machine.FireAsync(OrderTrigger.Submit);
```

## Complexity

- Configuration lookup: `O(1)` average
- Transition firing: `O(1)` average excluding user callbacks
- Entry/exit execution: `O(n)` for `n` callbacks on the active states
