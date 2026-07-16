using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;

/// <summary>
/// Provides metadata and payload of the currently executing job within its DI scope.
/// Inject into your <see cref="IExcelJob"/> implementation to read the payload.
/// </summary>
public interface ICurrentJobContext
{
    Guid    JobId             { get; }
    string  JobType           { get; }
    string? SerializedPayload { get; }
    int     RetryCount        { get; }

    /// <summary>
    /// Deserializes the JSON payload to <typeparamref name="T"/>.
    /// Returns null when the payload is empty or not set.
    /// </summary>
    T? GetPayload<T>() where T : class;
}
