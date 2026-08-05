# Scheduler

Reusable job scheduler for delayed, recurring, and cron-driven execution with priority ordering and retries.

## Folder structure

```text
Shared/Patterns/Scheduler
├── Abstractions
├── Builders
├── Configuration
├── Extensions
├── Models
└── README.md
```

## Capabilities

- delayed jobs
- recurring interval jobs
- cron schedules
- priority ordering
- retry delay strategy
- cancellation support
- DI registration via `AddSchedulerFramework`

## Example

```csharp
var scheduler = new SchedulerBuilder<SendEmailJob>()
    .WithHandler(new SendEmailHandler())
    .WithRetryDelayStrategy(attempt => TimeSpan.FromSeconds(attempt * 5))
    .Build();

await scheduler.ScheduleDelayedAsync(job, TimeSpan.FromMinutes(5), priority: 10);
await scheduler.RunDueAsync();
```

## Complexity

- Schedule: `O(log n)`
- Dispatch due jobs: `O(k log n)` for `k` processed jobs
- Cron next-run lookup: bounded linear scan over future minutes
