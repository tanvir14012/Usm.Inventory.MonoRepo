namespace Shared.AI.Prompting;

using System.Text.RegularExpressions;

/// <summary>
/// Prompt template engine for variable substitution and formatting.
/// </summary>
public class PromptTemplate
{
    private readonly string _template;
    private readonly HashSet<string> _variables;
    private readonly Regex _variablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a prompt template.
    /// </summary>
    public PromptTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
            throw new ArgumentException("Template cannot be empty", nameof(template));

        _template = template;
        _variables = ExtractVariables(template);
    }

    /// <summary>
    /// Gets the variable names used in the template.
    /// </summary>
    public IReadOnlySet<string> Variables => _variables;

    /// <summary>
    /// Formats the template with given values.
    /// </summary>
    public string Format(Dictionary<string, object> values)
    {
        var result = _template;

        foreach (var match in _variablePattern.Matches(result).Cast<Match>().Reverse())
        {
            var varName = match.Groups[1].Value;
            if (values.TryGetValue(varName, out var value))
            {
                result = result.Remove(match.Index, match.Length)
                    .Insert(match.Index, value?.ToString() ?? string.Empty);
            }
        }

        return result;
    }

    /// <summary>
    /// Formats with anonymous object (uses reflection).
    /// </summary>
    public string Format(object values)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in values.GetType().GetProperties())
        {
            dict[prop.Name] = prop.GetValue(values) ?? string.Empty;
        }

        return Format(dict);
    }

    /// <summary>
    /// Creates a builder for fluent template construction.
    /// </summary>
    public static PromptTemplateBuilder Builder() => new();

    private HashSet<string> ExtractVariables(string template)
    {
        var variables = new HashSet<string>();
        foreach (Match match in _variablePattern.Matches(template))
        {
            variables.Add(match.Groups[1].Value);
        }

        return variables;
    }
}

/// <summary>
/// Fluent builder for creating prompt templates.
/// </summary>
public class PromptTemplateBuilder
{
    private readonly List<string> _parts = new();
    private readonly HashSet<string> _variables = new();

    public PromptTemplateBuilder AddText(string text)
    {
        _parts.Add(text);
        return this;
    }

    public PromptTemplateBuilder AddVariable(string name, string? placeholder = null)
    {
        _variables.Add(name);
        _parts.Add($"{{{{{name}}}}}");
        return this;
    }

    public PromptTemplateBuilder AddSection(string sectionName, string content)
    {
        _parts.Add($"\n## {sectionName}\n{content}");
        return this;
    }

    public PromptTemplateBuilder AddExample(string input, string output)
    {
        _parts.Add($"\nExample:\nInput: {input}\nOutput: {output}");
        return this;
    }

    public PromptTemplate Build()
    {
        var template = string.Join("\n", _parts);
        return new PromptTemplate(template);
    }
}

/// <summary>
/// Prompt cache for storing and reusing generated prompts.
/// </summary>
public class PromptCache
{
    private readonly Dictionary<string, CachedPrompt> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _maxSize;

    public PromptCache(int maxSize = 1000)
    {
        _maxSize = maxSize;
    }

    /// <summary>
    /// Cached prompt entry.
    /// </summary>
    public class CachedPrompt
    {
        public required string Template { get; set; }
        public required Dictionary<string, object> Values { get; set; }
        public required string Result { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int HitCount { get; set; }
    }

    /// <summary>
    /// Gets or generates a prompt.
    /// </summary>
    public string GetOrGenerate(
        string key,
        PromptTemplate template,
        Dictionary<string, object> values)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                _lock.EnterWriteLock();
                try
                {
                    cached.HitCount++;
                    return cached.Result;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            _lock.EnterWriteLock();
            try
            {
                var result = template.Format(values);

                // Evict old entries if cache is full
                if (_cache.Count >= _maxSize)
                {
                    var oldest = _cache.Values.OrderBy(x => x.CreatedAt).First();
                    _cache.Remove(_cache.First(x => x.Value == oldest).Key);
                }

                _cache[key] = new CachedPrompt
                {
                    Template = template.Variables.First(),
                    Values = values,
                    Result = result
                };

                return result;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public (int size, int hits) GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            var hits = _cache.Values.Sum(x => x.HitCount);
            return (_cache.Count, hits);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}

/// <summary>
/// Common prompt templates for standard tasks.
/// </summary>
public static class CommonPrompts
{
    public static PromptTemplate Classification() =>
        new PromptTemplate("Classify the following text into one of these categories: {{categories}}\n\nText: {{text}}\n\nClassification:");

    public static PromptTemplate Summarization() =>
        new PromptTemplate("Summarize the following text in {{summary_length}} sentences:\n\n{{text}}\n\nSummary:");

    public static PromptTemplate QuestionAnswering() =>
        new PromptTemplate("Based on the following context:\n\n{{context}}\n\nAnswer this question: {{question}}\n\nAnswer:");

    public static PromptTemplate Sentiment() =>
        new PromptTemplate("Determine the sentiment (positive, negative, neutral) of the following text:\n\n{{text}}\n\nSentiment:");

    public static PromptTemplate NER() =>
        new PromptTemplate("Extract named entities from the following text:\n\n{{text}}\n\nEntities:");

    public static PromptTemplate Translation() =>
        new PromptTemplate("Translate the following text from {{source_language}} to {{target_language}}:\n\n{{text}}\n\nTranslation:");

    public static PromptTemplate CodeGeneration() =>
        new PromptTemplate("Generate {{language}} code that {{description}}\n\nCode:");

    public static PromptTemplate TextGeneration() =>
        new PromptTemplate("Generate a {{style}} about {{topic}}:\n\nGenerated text:");
}
