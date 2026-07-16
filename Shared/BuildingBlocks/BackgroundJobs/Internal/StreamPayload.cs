namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;

/// <summary>
/// Wraps an Excel stream as a Base64 JSON payload that can be stored in the job outbox
/// and later deserialized by the job implementation via <c>ICurrentJobContext.GetPayload&lt;StreamPayload&gt;()</c>.
/// </summary>
public sealed record StreamPayload(string Base64Data)
{
    /// <summary>Reconstructs the original Excel stream.</summary>
    public Stream ToStream() => new MemoryStream(Convert.FromBase64String(Base64Data));
}
