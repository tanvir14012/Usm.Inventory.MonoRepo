namespace Usm.Shared.Infrastructure.CDN.Models;

/// <summary>
/// Parameters for on-the-fly media transformation passed through the distribution context.
/// Populated from URL query parameters: ?w=800&amp;h=600&amp;fmt=webp&amp;q=80
/// </summary>
public sealed class MediaProcessingRequest
{
    /// <summary>Target width in pixels (0 = unconstrained).</summary>
    public int? Width { get; init; }

    /// <summary>Target height in pixels (0 = unconstrained).</summary>
    public int? Height { get; init; }

    public ImageResizeMode Mode { get; init; } = ImageResizeMode.Max;

    /// <summary>Desired output format: "webp", "avif", "jpeg", "png". Null = keep original.</summary>
    public string? OutputFormat { get; init; }

    /// <summary>Output quality 1–100. Null = use the configured default for the format.</summary>
    public int? Quality { get; init; }

    /// <summary>When true, auto-select the best format based on the request Accept header.</summary>
    public bool AutoFormat { get; init; } = true;

    /// <summary>
    /// Stable cache-key suffix derived from all transformation parameters.
    /// Null when no transformation is requested (passthrough).
    /// </summary>
    public string? CacheKey
    {
        get
        {
            if (Width is null && Height is null && OutputFormat is null && Quality is null)
                return null;
            return $"w{Width}_h{Height}_{Mode}_{OutputFormat ?? "orig"}_{Quality ?? 0}";
        }
    }
}

public enum ImageResizeMode
{
    /// <summary>Scale down uniformly to fit within Width × Height. Never upscales.</summary>
    Max,
    /// <summary>Crop to exact Width × Height from the centre.</summary>
    Crop,
    /// <summary>Pad to exact Width × Height with transparent/white fill.</summary>
    Pad,
    /// <summary>Stretch to exact Width × Height ignoring aspect ratio.</summary>
    Stretch
}
