using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Usm.Shared.Infrastructure.CDN.Abstractions;
using Usm.Shared.Infrastructure.CDN.Models;
using Usm.Shared.Infrastructure.CDN.Options;

// Disambiguate our domain enum from ImageSharp's
using IS = SixLabors.ImageSharp;
using ISProcessing = SixLabors.ImageSharp.Processing;

namespace Usm.Shared.Infrastructure.CDN.Media;

/// <summary>
/// On-the-fly image resizing and format conversion using SixLabors.ImageSharp.
///
/// Handles:
///   • Resize (width / height / mode): Max, Crop, Pad, Stretch
///   • Format conversion: JPEG, PNG, WebP (AVIF requires optional codec package)
///   • Quality settings per format
///   • Auto-format selection based on Accept header (set OutputFormat before calling)
///
/// The processed byte[] is returned to the EdgeProcessingStrategy which stores it in Redis
/// so subsequent requests for the same variant are served entirely from cache.
/// </summary>
internal sealed class AdaptiveImageProcessor(
    IOptions<CdnOptions> options,
    ILogger<AdaptiveImageProcessor> logger) : IMediaProcessor
{
    private readonly MediaProcessingOptions _opts = options.Value.MediaProcessing;

    public bool CanProcess(string contentType, MediaProcessingRequest request)
        => _opts.SupportedImageTypes.Any(
               t => t.Equals(contentType, StringComparison.OrdinalIgnoreCase))
           && request.CacheKey is not null; // Only intercept when a transformation is requested

    public async ValueTask<ProcessedMedia> ProcessAsync(
        Stream input,
        string sourceContentType,
        MediaProcessingRequest request,
        CancellationToken ct = default)
    {
        using var image = await IS.Image.LoadAsync(input, ct).ConfigureAwait(false);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Guard against oversized outputs
        var targetW = Math.Min(request.Width ?? image.Width, _opts.MaxImageWidth);
        var targetH = Math.Min(request.Height ?? image.Height, _opts.MaxImageHeight);

        if (targetW != image.Width || targetH != image.Height)
            Resize(image, targetW, targetH, request.Mode);

        var format = ResolveFormat(request.OutputFormat, sourceContentType);
        var encoder = BuildEncoder(format, request.Quality);

        using var ms = new MemoryStream();
        await image.SaveAsync(ms, encoder, ct).ConfigureAwait(false);
        var data = ms.ToArray();

        logger.LogDebug(
            "Processed image: {Orig}x{Orig2} → {W}x{H} [{Format}] {Bytes} bytes",
            originalWidth, originalHeight, image.Width, image.Height, format, data.Length);

        return new ProcessedMedia
        {
            Data = data,
            ContentType = MimeType(format),
            Width = image.Width,
            Height = image.Height,
            CacheKey = request.CacheKey
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void Resize(IS.Image image, int w, int h, ImageResizeMode mode)
    {
        var opts = new ResizeOptions
        {
            Size = new IS.Size(w, h),
            Mode = mode switch
            {
                ImageResizeMode.Crop => ISProcessing.ResizeMode.Crop,
                ImageResizeMode.Pad => ISProcessing.ResizeMode.Pad,
                ImageResizeMode.Stretch => ISProcessing.ResizeMode.Stretch,
                _ => ISProcessing.ResizeMode.Max
            }
        };
        image.Mutate(ctx => ctx.Resize(opts));
    }

    private string ResolveFormat(string? requested, string sourceContentType)
    {
        if (!string.IsNullOrEmpty(requested))
            return requested.ToLowerInvariant();

        if (_opts.EnableWebPConversion)
            return "webp";

        return sourceContentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpeg"
        };
    }

    private IS.Formats.IImageEncoder BuildEncoder(string format, int? quality) =>
        format switch
        {
            "webp" => new WebpEncoder { Quality = quality ?? _opts.DefaultWebPQuality },
            "png" => new PngEncoder(),
            _ => new JpegEncoder { Quality = quality ?? _opts.DefaultJpegQuality }
        };

    private static string MimeType(string format) =>
        format switch
        {
            "webp" => "image/webp",
            "png" => "image/png",
            _ => "image/jpeg"
        };
}
