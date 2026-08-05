# RuleEngine<TContext, TResult>

Reusable ordered rule engine with priorities, groups, expression trees, dynamic compilation, and async evaluation.

## Folder structure

```text
Shared/Patterns/RuleEngine
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
├── Models
└── README.md
```

## Capabilities

- priority ordering
- grouping
- expression-based rules
- lambda-based rules
- asynchronous rule evaluation
- compiled rule caching
- DI registration via `AddRuleEngineFramework`

## Example

```csharp
var engine = RuleEngine<OrderContext, string>.CreateBuilder()
    .WhenExpression(ctx => ctx.Total >= 100, ctx => "High", priority: 10, group: "risk")
    .WhenPredicate(ctx => ctx.Total >= 50, ctx => "Medium", priority: 5, group: "risk")
    .OtherwisePredicate(ctx => "Low", group: "risk")
    .Build();

var label = engine.Evaluate(order, group: "risk");
```

## Complexity

- Rule registration: `O(1)`
- Evaluation: `O(n)` for `n` matching rules in priority order
- Expression compilation: `O(n)` in rule count and expression size
- Cache lookup: `O(1)` average
