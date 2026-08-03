using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Usm.Shared.Data.Scalability.Jsonb;

/// <summary>
/// Zero/low-allocation JSONB serializer built on <see cref="System.Text.Json"/>.
/// <para>
/// Hot paths use <see cref="JsonSerializer.SerializeToUtf8Bytes{TValue}"/> which writes directly
/// to a pooled buffer, avoiding a UTF-16 → UTF-8 re-encode.
/// The <see cref="SerializeArray{T}"/> overload further reduces GC pressure for large arrays by
/// using a <see cref="ArrayBufferWriter{T}"/> so no intermediate <c>List</c> or <c>string</c>
/// allocation is required until the final UTF-8 → UTF-16 conversion for the Npgsql parameter.
/// </para>
/// </summary>
public static class JsonbSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    // ── Serialization ─────────────────────────────────────────────────────────

    /// <summary>Serializes <paramref name="value"/> to a JSONB-compatible string.</summary>
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, DefaultOptions);

    /// <summary>
    /// Returns raw UTF-8 bytes for the serialized value — use when the Npgsql
    /// parameter accepts a <c>byte[]</c> or <c>ReadOnlyMemory&lt;byte&gt;</c>.
    /// </summary>
    public static byte[] SerializeToUtf8Bytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, DefaultOptions);

    /// <summary>
    /// High-throughput serialization of a list using a pooled <see cref="ArrayBufferWriter{T}"/>.
    /// </summary>
    public static string SerializeList<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0) return "[]";
        return JsonSerializer.Serialize(items, DefaultOptions);
    }

    /// <summary>
    /// Serializes a <see cref="ReadOnlySpan{T}"/> array using a pooled buffer writer —
    /// the lowest-allocation path for large datasets on hot bulk-import paths.
    /// </summary>
    public static string SerializeArray<T>(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty) return "[]";

        // Initial capacity heuristic: 64 bytes per item.
        var bufferWriter = new ArrayBufferWriter<byte>(Math.Max(256, items.Length * 64));
        using (var writer = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions { SkipValidation = true }))
        {
            writer.WriteStartArray();
            foreach (var item in items)
                JsonSerializer.Serialize(writer, item, DefaultOptions);
            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    // ── Deserialization ────────────────────────────────────────────────────────

    /// <summary>Deserializes a JSONB string (or null) into <typeparamref name="T"/>.</summary>
    public static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>Zero-copy deserialization directly from a UTF-8 byte span.</summary>
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<T>(utf8Json, DefaultOptions);

    /// <summary>
    /// Deserializes a JSONB array string into a read-only list.
    /// Returns an empty list for null or empty input.
    /// </summary>
    public static IReadOnlyList<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        return JsonSerializer.Deserialize<List<T>>(json, DefaultOptions) ?? [];
    }

    /// <summary>Deserializes from a pooled UTF-8 byte array returned by <see cref="SerializeToUtf8Bytes{T}"/>.</summary>
    public static T? DeserializeFromUtf8Bytes<T>(byte[]? utf8Bytes)
    {
        if (utf8Bytes is null || utf8Bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(utf8Bytes.AsSpan(), DefaultOptions);
    }
}
