namespace Usm.Shared.Infrastructure.CDN.Options;

/// <summary>Options governing on-the-fly image and video processing at the CDN edge.</summary>
public sealed class MediaProcessingOptions
{
    public int MaxImageWidth { get; set; } = 4096;
    public int MaxImageHeight { get; set; } = 4096;

    /// <summary>JPEG output quality (1–100).</summary>
    public int DefaultJpegQuality { get; set; } = 85;

    /// <summary>WebP output quality (1–100).</summary>
    public int DefaultWebPQuality { get; set; } = 80;

    public bool EnableWebPConversion { get; set; } = true;

    /// <summary>AVIF requires SixLabors.ImageSharp.Formats.Avif (separate package) – disabled by default.</summary>
    public bool EnableAvifConversion { get; set; } = false;

    /// <summary>Redis cache key prefix for storing processed image variant metadata.</summary>
    public string ImageVariantCachePrefix { get; set; } = "cdn:img:variant";

    /// <summary>How long processed image variants are kept in Redis.</summary>
    public TimeSpan VariantCacheTtl { get; set; } = TimeSpan.FromDays(7);

    /// <summary>MIME types this processor handles.</summary>
    public string[] SupportedImageTypes { get; set; } =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp"
    ];

    /// <summary>Video MIME types eligible for byte-range streaming.</summary>
    public string[] SupportedVideoTypes { get; set; } =
    [
        "video/mp4", "video/webm", "application/x-mpegURL", "video/mp2t"
    ];
}
