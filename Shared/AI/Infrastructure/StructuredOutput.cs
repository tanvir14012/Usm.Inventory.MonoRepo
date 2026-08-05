namespace Shared.AI.Infrastructure;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Shared.AI.Abstractions;

/// <summary>
/// Handles structured JSON output from LLM providers using JSON Schema.
/// </summary>
public class StructuredOutputHandler
{
    private readonly ILogger? _logger;

    public StructuredOutputHandler(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates JSON Schema from a C# type.
    /// </summary>
    public JsonSchema GenerateSchema<T>(string description = "")
    {
        var type = typeof(T);
        var schema = GenerateSchemaForType(type, description);
        _logger?.LogDebug("Generated JSON schema for {Type}", type.Name);
        return schema;
    }

    /// <summary>
    /// Parses structured output into the target type.
    /// </summary>
    public T ParseStructuredOutput<T>(string json) where T : class
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            if (result == null)
                throw new InvalidOperationException("Deserialization resulted in null");

            _logger?.LogDebug("Successfully parsed structured output for {Type}", typeof(T).Name);
            return result;
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Failed to parse structured output");
            throw new InvalidOperationException($"Invalid JSON structure: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates JSON against a schema.
    /// </summary>
    public bool ValidateAgainstSchema<T>(string json) where T : class
    {
        try
        {
            ParseStructuredOutput<T>(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts JSON from LLM response that may contain additional text.
    /// </summary>
    public string ExtractJson(string response)
    {
        // Look for JSON object or array
        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
            return response[jsonStart..(jsonEnd + 1)];

        jsonStart = response.IndexOf('[');
        jsonEnd = response.LastIndexOf(']');

        if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
            return response[jsonStart..(jsonEnd + 1)];

        throw new InvalidOperationException("No valid JSON found in response");
    }

    private JsonSchema GenerateSchemaForType(Type type, string description = "")
    {
        var schema = new JsonSchema
        {
            Type = "object",
            Description = description,
            Properties = new Dictionary<string, JsonSchema>()
        };

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            schema.Type = "array";
            var itemType = type.GetGenericArguments()[0];
            schema.Items = GenerateSchemaForType(itemType);
            return schema;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyType = property.PropertyType;
            var jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var propertyName = jsonPropertyAttr?.Name ?? property.Name;

            schema.Properties![propertyName] = GeneratePropertySchema(propertyType, property);
        }

        return schema;
    }

    private JsonSchema GeneratePropertySchema(Type type, PropertyInfo property)
    {
        var isNullable = Nullable.GetUnderlyingType(type) != null;
        var baseType = Nullable.GetUnderlyingType(type) ?? type;

        var schema = new JsonSchema();

        if (baseType == typeof(string))
            schema.Type = "string";
        else if (baseType == typeof(int) || baseType == typeof(long))
            schema.Type = "integer";
        else if (baseType == typeof(float) || baseType == typeof(double) || baseType == typeof(decimal))
            schema.Type = "number";
        else if (baseType == typeof(bool))
            schema.Type = "boolean";
        else if (baseType == typeof(DateTime))
        {
            schema.Type = "string";
            schema.Format = "date-time";
        }
        else if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
        {
            schema.Type = "array";
            var itemType = baseType.GetGenericArguments()[0];
            schema.Items = GenerateSchemaForType(itemType);
        }
        else if (baseType.IsClass)
        {
            schema = GenerateSchemaForType(baseType);
        }

        var descriptionAttr = property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        if (descriptionAttr != null)
            schema.Description = descriptionAttr.Description;

        return schema;
    }
}

/// <summary>
/// JSON Schema representation for structured output validation.
/// </summary>
public class JsonSchema
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, JsonSchema>? Properties { get; set; }

    [JsonPropertyName("items")]
    public JsonSchema? Items { get; set; }

    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }

    [JsonPropertyName("minimum")]
    public decimal? Minimum { get; set; }

    [JsonPropertyName("maximum")]
    public decimal? Maximum { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }
}

/// <summary>
/// Validates structured outputs and provides type-safe extraction.
/// </summary>
public static class StructuredOutputExtensions
{
    /// <summary>
    /// Extracts structured output from a chat response.
    /// </summary>
    public static T ExtractStructured<T>(this ChatResponse response, StructuredOutputHandler handler) where T : class
    {
        var json = handler.ExtractJson(response.Content);
        return handler.ParseStructuredOutput<T>(json);
    }

    /// <summary>
    /// Creates a prompt instruction for structured output.
    /// </summary>
    public static string GetStructuredOutputInstruction<T>(this StructuredOutputHandler handler, string context = "") where T : class
    {
        var schema = handler.GenerateSchema<T>();
        var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });

        return $@"{context}

Please respond with ONLY valid JSON matching this schema, no additional text:

{schemaJson}";
    }
}

/// <summary>
/// Common structured output types for chat applications.
/// </summary>
public static class StructuredOutputTypes
{
    /// <summary>
    /// Simple classification result.
    /// </summary>
    public class Classification
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }
    }

    /// <summary>
    /// Entity extraction result.
    /// </summary>
    public class EntityExtractionResult
    {
        [JsonPropertyName("entities")]
        public List<Entity> Entities { get; set; } = new();

        [JsonPropertyName("raw_text")]
        public string? RawText { get; set; }
    }

    public class Entity
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }
    }

    /// <summary>
    /// Sentiment analysis result.
    /// </summary>
    public class SentimentAnalysis
    {
        [JsonPropertyName("sentiment")]
        public string? Sentiment { get; set; } // positive, negative, neutral

        [JsonPropertyName("score")]
        public float Score { get; set; } // -1.0 to 1.0

        [JsonPropertyName("key_phrases")]
        public List<string> KeyPhrases { get; set; } = new();
    }

    /// <summary>
    /// Multi-choice question with answer.
    /// </summary>
    public class MultiChoiceAnswer
    {
        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("selected_option")]
        public string? SelectedOption { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    /// <summary>
    /// Structured fact extraction.
    /// </summary>
    public class FactExtraction
    {
        [JsonPropertyName("facts")]
        public List<Fact> Facts { get; set; } = new();

        [JsonPropertyName("source_text")]
        public string? SourceText { get; set; }
    }

    public class Fact
    {
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("predicate")]
        public string? Predicate { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Structured summary with key points.
    /// </summary>
    public class StructuredSummary
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("key_points")]
        public List<string> KeyPoints { get; set; } = new();

        [JsonPropertyName("word_count")]
        public int WordCount { get; set; }
    }
}
