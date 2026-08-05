# Plugin

Reusable plugin framework for assembly scanning, dependency ordering, versioned descriptors, and lifecycle hosting.

## Folder structure

```text
Shared/Patterns/Plugin
├── Abstractions
├── Builders
├── Extensions
├── Models
└── README.md
```

## Capabilities

- assembly scanning
- explicit plugin registration
- dependency graph ordering
- versioned descriptors
- lifecycle initialization and shutdown
- DI registration via `AddPluginFramework`

## Example

```csharp
var host = new PluginBuilder()
    .ScanAssembly(typeof(SomePlugin).Assembly)
    .WithContext(serviceProvider)
    .Build();

await host.InitializeAsync();
await host.ShutdownAsync();
```

## Complexity

- Discovery: `O(n)` for `n` types
- Ordering: `O(n + e)` for `n` plugins and `e` dependency edges
- Lifecycle: `O(n)`
