using Microsoft.Extensions.Logging;

namespace Usm.Shared.Infrastructure.CDN.Media;

/// <summary>
/// Handles HLS (HTTP Live Streaming) manifest and MPEG-TS segment delivery.
///
/// Full real-time transcoding requires an external process (FFmpeg) outside this library's scope.
/// This class provides:
///   • Content-type resolution for .m3u8 / .ts files
///   • Playlist URL rewriting to route segment URLs through the CDN base URL
///   • Detection helpers used by the byte-range streaming handler
/// </summary>
public sealed class HlsFragmentProcessor(ILogger<HlsFragmentProcessor> logger)
{
    private static readonly HashSet<string> HlsContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "application/x-mpegURL",
            "application/vnd.apple.mpegurl",
            "video/mp2t"
        };

    /// <summary>Returns true if the content-type represents an HLS manifest or segment.</summary>
    public static bool IsHlsContent(string contentType) => HlsContentTypes.Contains(contentType);

    /// <summary>Returns true for .m3u8 HLS playlist keys.</summary>
    public static bool IsPlaylist(string key) =>
        key.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true for .ts MPEG-TS segment keys.</summary>
    public static bool IsFragment(string key) =>
        key.EndsWith(".ts", StringComparison.OrdinalIgnoreCase);

    /// <summary>Resolves the correct Content-Type for an HLS file based on its key extension.</summary>
    public static string ResolveContentType(string key) => key switch
    {
        _ when key.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            => "application/x-mpegURL",
        _ when key.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
            => "video/mp2t",
        _ => "application/octet-stream"
    };

    /// <summary>
    /// Rewrites relative segment URLs in an HLS manifest to absolute CDN URLs so that
    /// media players can fetch segments directly from the CDN edge.
    /// </summary>
    /// <param name="playlistStream">Input stream of the .m3u8 manifest.</param>
    /// <param name="cdnBaseUrl">CDN base URL (e.g. https://cdn.example.com).</param>
    /// <param name="bucketPath">Path prefix representing the bucket/folder in the CDN URL space.</param>
    public async ValueTask<string> RewritePlaylistUrlsAsync(
        Stream playlistStream,
        string cdnBaseUrl,
        string bucketPath,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(playlistStream, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var cdnBase = cdnBaseUrl.TrimEnd('/');
        var pathPrefix = bucketPath.TrimStart('/');

        var rewritten = raw.Split('\n')
            .Select(line =>
            {
                var trimmed = line.TrimEnd('\r');

                // Leave M3U8 directives and empty lines unchanged
                if (trimmed.StartsWith('#') || string.IsNullOrWhiteSpace(trimmed))
                    return trimmed;

                // Absolute URLs (already rewritten or external) – leave as-is
                if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return trimmed;

                // Rewrite relative segment/playlist reference to absolute CDN URL
                var segment = trimmed.TrimStart('/');
                return string.IsNullOrEmpty(pathPrefix)
                    ? $"{cdnBase}/{segment}"
                    : $"{cdnBase}/{pathPrefix}/{segment}";
            });

        var result = string.Join('\n', rewritten);
        logger.LogDebug("HLS playlist rewritten for CDN base '{BaseUrl}'", cdnBaseUrl);
        return result;
    }
}
