using System.Text.Json;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;

internal sealed class CurrentJobContext(JobContextHolder holder) : ICurrentJobContext
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public Guid    JobId             => holder.Get().JobId;
    public string  JobType           => holder.Get().JobType;
    public string? SerializedPayload => holder.Get().SerializedPayload;
    public int     RetryCount        => holder.Get().RetryCount;

    public T? GetPayload<T>() where T : class
    {
        var payload = holder.Get().SerializedPayload;
        return string.IsNullOrEmpty(payload)
            ? null
            : JsonSerializer.Deserialize<T>(payload, JsonOpts);
    }
}
