using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Abstractions;

/// <summary>
/// Processes a raw media stream and produces a transformed output.
/// Register multiple implementations (image processor, video fragment handler)
/// and select via <see cref="CanProcess"/>.
/// </summary>
public interface IMediaProcessor
{
    /// <summary>Returns true if this processor handles the given MIME type and transformation request.</summary>
    bool CanProcess(string contentType, MediaProcessingRequest request);

    /// <summary>
    /// Transforms <paramref name="input"/> according to <paramref name="request"/> and returns
    /// the processed result.  Hot path – implementations must minimise allocations.
    /// </summary>
    ValueTask<ProcessedMedia> ProcessAsync(
        Stream input,
        string sourceContentType,
        MediaProcessingRequest request,
        CancellationToken ct = default);
}
