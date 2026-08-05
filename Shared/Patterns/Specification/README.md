# Specification<T>

Reusable, immutable business-rule composition for in-memory, queryable, and async evaluation.

## Folder structure

```text
Shared/Patterns/Specification
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Internal
└── README.md
```

## Capabilities

- `Where`, `And`, `Or`, `Not`
- `Compile` and `ToExpression`
- async evaluation with `CancellationToken`
- DI registration via `AddSpecificationFramework`
- expression caching through `SpecificationOptions`

## Example

```csharp
var adult = Specification<Person>.From(p => p.Age >= 18);
var active = Specification<Person>.From(p => p.IsActive);
var spec = adult.And(active.Not());

var filtered = people.Where(spec);
var query = db.People.Where(spec);
var compiled = spec.Compile();
```

## Complexity

- Composition: `O(1)`
- Synchronous evaluation: `O(n)` per sequence
- Expression compilation: `O(n)` in the expression tree size
- Async evaluation: `O(n)` per sequence
