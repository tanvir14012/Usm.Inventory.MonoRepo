namespace Shared.AI.Utilities;

/// <summary>
/// Utility class for token estimation and counting.
/// Provides basic token estimation for common models.
/// For accurate counts, use provider-specific tokenizers.
/// </summary>
public static class TokenEstimator
{
    private const int AvgTokensPerWord = 4;
    private const int AvgCharsPerToken = 4;

    /// <summary>
    /// Estimates token count for text using word-based heuristic.
    /// This is an approximation; actual count depends on tokenizer.
    /// </summary>
    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Simple heuristic: split by spaces
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return Math.Max(1, words.Length / AvgTokensPerWord);
    }

    /// <summary>
    /// Estimates token count based on character count.
    /// </summary>
    public static int EstimateTokenCountByChars(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Math.Max(1, text.Length / AvgCharsPerToken);
    }

    /// <summary>
    /// Estimates tokens for a list of messages.
    /// </summary>
    public static int EstimateTokenCount(IEnumerable<string> texts)
    {
        return texts.Sum(EstimateTokenCount);
    }

    /// <summary>
    /// Calculates if text fits within a token budget.
    /// </summary>
    public static bool FitsInBudget(string text, int maxTokens) =>
        EstimateTokenCount(text) <= maxTokens;
}

/// <summary>
/// String similarity algorithms.
/// </summary>
public static class StringSimilarity
{
    /// <summary>
    /// Calculates Levenshtein distance (edit distance) between two strings.
    /// </summary>
    /// <remarks>
    /// Returns the minimum number of single-character edits needed to transform one string into another.
    /// </remarks>
    public static int LevenshteinDistance(string s1, string s2)
    {
        if (s1 == null) throw new ArgumentNullException(nameof(s1));
        if (s2 == null) throw new ArgumentNullException(nameof(s2));

        if (s1.Length == 0) return s2.Length;
        if (s2.Length == 0) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    /// <summary>
    /// Calculates similarity ratio based on Levenshtein distance (0-1).
    /// </summary>
    public static double SimilarityRatio(string s1, string s2)
    {
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        
        if (maxLength == 0) return 1.0; // Both empty
        
        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Calculates Jaro similarity (better for short strings).
    /// </summary>
    public static double JaroSimilarity(string s1, string s2)
    {
        if (s1 == null || s2 == null)
            return s1 == s2 ? 1.0 : 0.0;

        if (s1.Length == 0 && s2.Length == 0)
            return 1.0;

        if (s1.Length == 0 || s2.Length == 0)
            return 0.0;

        var matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
        if (matchDistance < 0) matchDistance = 0;

        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];

        int matches = 0;
        int transpositions = 0;

        // Find matches
        for (int i = 0; i < s1.Length; i++)
        {
            var start = Math.Max(0, i - matchDistance);
            var end = Math.Min(i + matchDistance + 1, s2.Length);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j])
                    continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0.0;

        // Count transpositions
        int k = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i]) continue;

            while (!s2Matches[k])
                k++;

            if (s1[i] != s2[k])
                transpositions++;

            k++;
        }

        return (matches / (double)s1.Length +
                matches / (double)s2.Length +
                (matches - transpositions / 2.0) / matches) / 3.0;
    }

    /// <summary>
    /// Calculates Jaro-Winkler similarity (better for strings with matching prefixes).
    /// </summary>
    public static double JaroWinklerSimilarity(string s1, string s2, double prefixWeight = 0.1)
    {
        var jaro = JaroSimilarity(s1, s2);

        if (jaro < 0.7)
            return jaro;

        // Count common prefix
        int commonPrefix = 0;
        for (int i = 0; i < Math.Min(s1.Length, s2.Length) && s1[i] == s2[i]; i++)
            commonPrefix++;

        commonPrefix = Math.Min(commonPrefix, 4); // Cap at 4
        return jaro + commonPrefix * prefixWeight * (1 - jaro);
    }
}

/// <summary>
/// Algorithms for ranking and similarity calculations.
/// </summary>
public static class SimilarityAlgorithms
{
    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    public static double CosineSimilarity(ReadOnlySpan<float> vec1, ReadOnlySpan<float> vec2)
    {
        if (vec1.Length != vec2.Length)
            throw new ArgumentException("Vectors must have the same length");

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            magnitude1 += vec1[i] * vec1[i];
            magnitude2 += vec2[i] * vec2[i];
        }

        var denom = Math.Sqrt((double)magnitude1) * Math.Sqrt((double)magnitude2);
        if (denom == 0) return 0;

        return dotProduct / denom;
    }

    /// <summary>
    /// Calculates Euclidean distance between two vectors.
    /// </summary>
    public static double EuclideanDistance(ReadOnlySpan<float> vec1, ReadOnlySpan<float> vec2)
    {
        if (vec1.Length != vec2.Length)
            throw new ArgumentException("Vectors must have the same length");

        double sumSquaredDiff = 0;
        for (int i = 0; i < vec1.Length; i++)
        {
            var diff = vec1[i] - vec2[i];
            sumSquaredDiff += diff * diff;
        }

        return Math.Sqrt(sumSquaredDiff);
    }

    /// <summary>
    /// Calculates Manhattan distance between two vectors.
    /// </summary>
    public static double ManhattanDistance(ReadOnlySpan<float> vec1, ReadOnlySpan<float> vec2)
    {
        if (vec1.Length != vec2.Length)
            throw new ArgumentException("Vectors must have the same length");

        double sum = 0;
        for (int i = 0; i < vec1.Length; i++)
            sum += Math.Abs(vec1[i] - vec2[i]);

        return sum;
    }

    /// <summary>
    /// Selects top-k items by score.
    /// </summary>
    public static IEnumerable<(T Item, double Score)> TopK<T>(
        IEnumerable<(T item, double score)> items,
        int k)
    {
        return items
            .OrderByDescending(x => x.score)
            .Take(k);
    }
}

/// <summary>
/// Text chunking strategies for RAG.
/// </summary>
public static class TextChunking
{
    /// <summary>
    /// Chunks text into fixed-size windows with overlap.
    /// Good for documents, code, and structured text.
    /// </summary>
    public static IEnumerable<string> SlidingWindowChunk(
        string text,
        int chunkSize,
        int overlapSize = 0)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive");

        if (overlapSize < 0 || overlapSize >= chunkSize)
            throw new ArgumentException("Overlap must be between 0 and chunk size");

        var lines = text.Split(new[] { "\n\r", "\n", "\r" }, StringSplitOptions.None);
        var currentChunk = new System.Text.StringBuilder();
        var lastChunk = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var testContent = currentChunk.Length == 0 
                ? line 
                : currentChunk.ToString() + "\n" + line;

            if (testContent.Length <= chunkSize)
            {
                currentChunk.Append(currentChunk.Length == 0 ? line : "\n" + line);
            }
            else
            {
                if (currentChunk.Length > 0)
                {
                    lastChunk = currentChunk;
                    yield return currentChunk.ToString();
                }

                // Start new chunk with overlap from previous
                currentChunk = new System.Text.StringBuilder();
                if (overlapSize > 0 && lastChunk.Length > 0)
                {
                    var overlapContent = lastChunk.ToString();
                    if (overlapContent.Length > overlapSize)
                        overlapContent = overlapContent.Substring(overlapContent.Length - overlapSize);
                    
                    currentChunk.Append(overlapContent + "\n" + line);
                }
                else
                {
                    currentChunk.Append(line);
                }
            }
        }

        if (currentChunk.Length > 0)
            yield return currentChunk.ToString();
    }

    /// <summary>
    /// Splits text by paragraphs and then by size if needed.
    /// Better for long documents.
    /// </summary>
    public static IEnumerable<string> RecursiveChunk(
        string text,
        int maxChunkSize,
        string[] separators)
    {
        if (maxChunkSize <= 0)
            throw new ArgumentException("Max chunk size must be positive");

        if (separators == null || separators.Length == 0)
            separators = new[] { "\n\n", "\n", " " };

        var chunks = new List<string> { text };

        foreach (var separator in separators)
        {
            var newChunks = new List<string>();

            foreach (var chunk in chunks)
            {
                if (chunk.Length <= maxChunkSize)
                {
                    newChunks.Add(chunk);
                }
                else
                {
                    var splits = chunk.Split(separator);
                    var goodChunks = new List<string>();
                    var current = "";

                    foreach (var split in splits)
                    {
                        if ((current + split).Length <= maxChunkSize)
                        {
                            current += split + separator;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(current))
                                goodChunks.Add(current.TrimEnd(separator.ToCharArray()));

                            current = split + separator;
                        }
                    }

                    if (!string.IsNullOrEmpty(current))
                        goodChunks.Add(current.TrimEnd(separator.ToCharArray()));

                    newChunks.AddRange(goodChunks);
                }
            }

            chunks = newChunks;
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c));
    }
}
