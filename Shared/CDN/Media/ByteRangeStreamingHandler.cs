using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Usm.Shared.Infrastructure.CDN.Models;

namespace Usm.Shared.Infrastructure.CDN.Media;

/// <summary>
/// Delivers assets to HTTP clients using zero-copy byte-range streaming via
/// <see cref="System.IO.Pipelines"/> and <see cref="IHttpResponseBodyFeature"/>.
///
/// Key design points:
///   • Range header parsing supports single byte ranges and suffix ranges.
///   • Conditional GET (ETag / If-None-Match, Last-Modified / If-Modified-Since) handled before streaming.
///   • PipeWriter.GetMemory avoids intermediate byte[] allocations on the hot path.
///   • ArrayPool&lt;byte&gt; fallback for non-seekable streams (e.g. gzip-compressed S3 bodies).
/// </summary>
public sealed class ByteRangeStreamingHandler(ILogger<ByteRangeStreamingHandler> logger)
{
    private const int BufferSize = 65_536; // 64 KB pipe buffer

    /// <summary>Streams <paramref name="source"/> to the HTTP response, respecting Range and conditional headers.</summary>
    public async ValueTask StreamAsync(
        HttpContext context,
        Stream source,
        AssetMetadata metadata,
        CancellationToken ct = default)
    {
        SetCacheHeaders(context.Response, metadata);

        if (IsNotModified(context.Request, metadata))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        var rangeHeader = context.Request.Headers.Range.ToString();
        if (!string.IsNullOrEmpty(rangeHeader) &&
            TryParseRange(rangeHeader, metadata.Size, out var start, out var end))
        {
            await StreamRangeAsync(context, source, metadata, start, end, ct).ConfigureAwait(false);
        }
        else
        {
            await StreamFullAsync(context, source, metadata, ct).ConfigureAwait(false);
        }
    }

    // ── Range response (206 Partial Content) ────────────────────────────────

    private async ValueTask StreamRangeAsync(
        HttpContext context, Stream source, AssetMetadata metadata,
        long start, long end, CancellationToken ct)
    {
        var length = end - start + 1;

        context.Response.StatusCode = StatusCodes.Status206PartialContent;
        context.Response.ContentLength = length;
        context.Response.Headers.ContentRange = $"bytes {start}-{end}/{metadata.Size}";

        if (source.CanSeek)
            source.Seek(start, SeekOrigin.Begin);
        else
            await SkipAsync(source, start, ct).ConfigureAwait(false);

        await WriteToResponseAsync(context, source, length, ct).ConfigureAwait(false);
    }

    // ── Full response (200 OK) ───────────────────────────────────────────────

    private async ValueTask StreamFullAsync(
        HttpContext context, Stream source, AssetMetadata metadata, CancellationToken ct)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentLength = metadata.Size;
        await WriteToResponseAsync(context, source, metadata.Size, ct).ConfigureAwait(false);
    }

    // ── Pipeline write ───────────────────────────────────────────────────────

    private static async ValueTask WriteToResponseAsync(
        HttpContext context, Stream source, long maxBytes, CancellationToken ct)
    {
        var feature = context.Features.Get<IHttpResponseBodyFeature>();
        if (feature is not null)
        {
            await feature.StartAsync(ct).ConfigureAwait(false);
            await PumpToPipeAsync(source, feature.Writer, maxBytes, ct).ConfigureAwait(false);
        }
        else
        {
            await PumpToStreamAsync(source, context.Response.Body, maxBytes, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Zero-allocation pump: reads directly into PipeWriter's managed memory segment.</summary>
    private static async ValueTask PumpToPipeAsync(
        Stream source, PipeWriter writer, long maxBytes, CancellationToken ct)
    {
        long remaining = maxBytes;
        try
        {
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();

                var bufLen = (int)Math.Min(BufferSize, remaining);
                var mem = writer.GetMemory(bufLen);

                var read = await source.ReadAsync(mem[..bufLen], ct).ConfigureAwait(false);
                if (read == 0)
                    break;

                writer.Advance(read);
                remaining -= read;

                var flush = await writer.FlushAsync(ct).ConfigureAwait(false);
                if (flush.IsCompleted || flush.IsCanceled)
                    break;
            }
        }
        finally
        {
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>ArrayPool-backed fallback for non-pipeline response bodies.</summary>
    private static async ValueTask PumpToStreamAsync(
        Stream source, Stream dest, long maxBytes, CancellationToken ct)
    {
        var buf = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            long remaining = maxBytes;
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var toRead = (int)Math.Min(buf.Length, remaining);
                var read = await source.ReadAsync(buf.AsMemory(0, toRead), ct).ConfigureAwait(false);
                if (read == 0)
                    break;
                await dest.WriteAsync(buf.AsMemory(0, read), ct).ConfigureAwait(false);
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    /// <summary>Skips bytes in a non-seekable stream using a rented buffer.</summary>
    private static async ValueTask SkipAsync(Stream source, long count, CancellationToken ct)
    {
        var buf = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            long remaining = count;
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var toRead = (int)Math.Min(buf.Length, remaining);
                var read = await source.ReadAsync(buf.AsMemory(0, toRead), ct).ConfigureAwait(false);
                if (read == 0)
                    throw new IOException($"Stream ended {remaining} bytes before the requested range start.");
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    // ── HTTP header helpers ──────────────────────────────────────────────────

    private static void SetCacheHeaders(HttpResponse response, AssetMetadata metadata)
    {
        response.Headers.AcceptRanges = "bytes";
        response.ContentType = metadata.ContentType;
        if (metadata.ETag is not null)
            response.Headers.ETag = $"\"{metadata.ETag}\"";
        response.Headers.LastModified = metadata.LastModified.ToString("R");
    }

    private static bool IsNotModified(HttpRequest request, AssetMetadata metadata)
    {
        var ifNoneMatch = request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && metadata.ETag is not null)
        {
            var clientTag = ifNoneMatch.Trim('"');
            return clientTag == "*" || clientTag == metadata.ETag;
        }

        var ifModifiedSince = request.Headers.IfModifiedSince.ToString();
        if (!string.IsNullOrEmpty(ifModifiedSince) &&
            DateTimeOffset.TryParse(ifModifiedSince, out var since))
            return metadata.LastModified <= since;

        return false;
    }

    /// <summary>
    /// Parses a single "bytes=start-end", "bytes=start-", or "bytes=-suffix" range.
    /// Multi-range requests are not supported (returns false; caller falls back to 200 OK).
    /// </summary>
    private static bool TryParseRange(
        string header, long contentLength, out long start, out long end)
    {
        start = 0;
        end = contentLength - 1;

        if (!header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return false;

        var range = header.AsSpan(6);
        var dash = range.IndexOf('-');
        if (dash < 0)
            return false;

        var startSpan = range[..dash];
        var endSpan = range[(dash + 1)..];

        if (startSpan.IsEmpty)
        {
            // Suffix range: bytes=-N  → last N bytes
            if (!long.TryParse(endSpan, out var suffix) || suffix <= 0)
                return false;
            start = Math.Max(0, contentLength - suffix);
            end = contentLength - 1;
        }
        else
        {
            if (!long.TryParse(startSpan, out start))
                return false;
            end = endSpan.IsEmpty
                ? contentLength - 1
                : long.TryParse(endSpan, out var parsed) ? parsed : contentLength - 1;
        }

        end = Math.Min(end, contentLength - 1);
        return start >= 0 && start <= end;
    }
}
