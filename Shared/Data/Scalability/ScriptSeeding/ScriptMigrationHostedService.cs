using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Usm.Shared.Data.Scalability.Abstractions;

namespace Usm.Shared.Data.Scalability.ScriptSeeding;

/// <summary>
/// Hosted service that runs <see cref="IScriptMigrationEngine"/> once at startup,
/// before the application begins accepting traffic.
/// If script execution fails the host startup is aborted so the service never
/// starts in an inconsistent database state.
/// </summary>
public sealed class ScriptMigrationHostedService(
    IScriptMigrationEngine engine,
    ILogger<ScriptMigrationHostedService> logger) : IHostedService
{
    private readonly IScriptMigrationEngine _engine = engine;
    private readonly ILogger<ScriptMigrationHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting idempotent SQL script seeding…");
        try
        {
            await _engine.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("SQL script seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SQL script seeding failed — aborting host startup to prevent inconsistent state.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
