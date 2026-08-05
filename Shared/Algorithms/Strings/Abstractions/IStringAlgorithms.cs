namespace Usm.Shared.Algorithms.Strings.Abstractions;

/// <summary>
/// Represents reusable string matching and distance algorithms.
/// </summary>
public interface IStringAlgorithms
{
    /// <summary>Performs Knuth-Morris-Pratt string matching.</summary>
    int KmpSearch(string text, string pattern);

    /// <summary>Performs Rabin-Karp string matching.</summary>
    int RabinKarpSearch(string text, string pattern);

    /// <summary>Performs Boyer-Moore string matching.</summary>
    int BoyerMooreSearch(string text, string pattern);

    /// <summary>Computes Levenshtein distance.</summary>
    int LevenshteinDistance(string s1, string s2);

    /// <summary>Computes Damerau-Levenshtein distance.</summary>
    int DamerauLevenshteinDistance(string s1, string s2);

    /// <summary>Finds the longest common subsequence length.</summary>
    int LongestCommonSubsequenceLength(string s1, string s2);

    /// <summary>Searches asynchronously.</summary>
    ValueTask<int> KmpSearchAsync(string text, string pattern, CancellationToken cancellationToken = default);

    /// <summary>Computes distance asynchronously.</summary>
    ValueTask<int> LevenshteinDistanceAsync(string s1, string s2, CancellationToken cancellationToken = default);
}
