using Usm.Shared.Algorithms.Strings.Abstractions;

namespace Usm.Shared.Algorithms.Strings.Implementation;

/// <summary>
/// String matching and distance algorithms.
/// </summary>
public sealed class StringAlgorithms : IStringAlgorithms
{
    /// <inheritdoc />
    public int KmpSearch(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0 || pattern.Length > text.Length)
            return -1;

        var lps = ComputeLps(pattern);
        var j = 0;
        for (var i = 0; i < text.Length; i++)
        {
            while (j > 0 && text[i] != pattern[j])
                j = lps[j - 1];

            if (text[i] == pattern[j])
                j++;

            if (j == pattern.Length)
                return i - j + 1;
        }

        return -1;
    }

    /// <inheritdoc />
    public int RabinKarpSearch(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0 || pattern.Length > text.Length)
            return -1;

        const int prime = 101;
        const int base_ = 256;
        var patternHash = 0;
        var textHash = 0;
        var baseValue = 1;

        for (var i = 0; i < pattern.Length - 1; i++)
            baseValue = (baseValue * base_) % prime;

        for (var i = 0; i < pattern.Length; i++)
        {
            patternHash = (patternHash * base_ + pattern[i]) % prime;
            textHash = (textHash * base_ + text[i]) % prime;
        }

        for (var i = 0; i <= text.Length - pattern.Length; i++)
        {
            if (patternHash == textHash)
            {
                var match = true;
                for (var j = 0; j < pattern.Length; j++)
                {
                    if (text[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }

            if (i < text.Length - pattern.Length)
            {
                textHash = (base_ * (textHash - text[i] * baseValue) + text[i + pattern.Length]) % prime;
                if (textHash < 0)
                    textHash += prime;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    public int BoyerMooreSearch(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0 || pattern.Length > text.Length)
            return -1;

        var badChar = new Dictionary<char, int>();
        for (var i = 0; i < pattern.Length; i++)
            badChar[pattern[i]] = i;

        var i_text = 0;
        while (i_text <= text.Length - pattern.Length)
        {
            var j = pattern.Length - 1;
            while (j >= 0 && pattern[j] == text[i_text + j])
                j--;

            if (j < 0)
                return i_text;

            var badCharShift = badChar.TryGetValue(text[i_text + j], out var pos) ? j - pos : j + 1;
            i_text += Math.Max(1, badCharShift);
        }

        return -1;
    }

    /// <inheritdoc />
    public int LevenshteinDistance(string s1, string s2)
    {
        ArgumentNullException.ThrowIfNull(s1);
        ArgumentNullException.ThrowIfNull(s2);

        var m = s1.Length;
        var n = s2.Length;
        var dp = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++)
            dp[i, 0] = i;
        for (var j = 0; j <= n; j++)
            dp[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                if (s1[i - 1] == s2[j - 1])
                    dp[i, j] = dp[i - 1, j - 1];
                else
                    dp[i, j] = 1 + Math.Min(dp[i - 1, j], Math.Min(dp[i, j - 1], dp[i - 1, j - 1]));
            }
        }

        return dp[m, n];
    }

    /// <inheritdoc />
    public int DamerauLevenshteinDistance(string s1, string s2)
    {
        ArgumentNullException.ThrowIfNull(s1);
        ArgumentNullException.ThrowIfNull(s2);

        var m = s1.Length;
        var n = s2.Length;
        var da = new Dictionary<char, int>();
        var maxDist = m + n;
        var h = new int[m + 2, n + 2];

        h[0, 0] = maxDist;
        for (var i = 0; i <= m; i++)
        {
            h[i + 1, 0] = maxDist;
            h[i + 1, 1] = i;
        }
        for (var j = 0; j <= n; j++)
        {
            h[0, j + 1] = maxDist;
            h[1, j + 1] = j;
        }

        for (var i = 1; i <= m; i++)
        {
            var db = 0;
            for (var j = 1; j <= n; j++)
            {
                var k = da.TryGetValue(s2[j - 1], out var val) ? val : 0;
                var l = db;
                var cost = 1;
                if (s1[i - 1] == s2[j - 1])
                {
                    cost = 0;
                    db = j;
                }

                h[i + 1, j + 1] = Math.Min(
                    Math.Min(h[i, j] + cost, h[i + 1, j] + 1),
                    Math.Min(h[i, j + 1] + 1, h[k, l] + (i - k - 1) + 1 + (j - l - 1)));
            }

            da[s1[i - 1]] = i;
        }

        return h[m + 1, n + 1];
    }

    /// <inheritdoc />
    public int LongestCommonSubsequenceLength(string s1, string s2)
    {
        ArgumentNullException.ThrowIfNull(s1);
        ArgumentNullException.ThrowIfNull(s2);

        var m = s1.Length;
        var n = s2.Length;
        var dp = new int[m + 1, n + 1];

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                if (s1[i - 1] == s2[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        return dp[m, n];
    }

    /// <inheritdoc />
    public ValueTask<int> KmpSearchAsync(string text, string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(KmpSearch(text, pattern));
    }

    /// <inheritdoc />
    public ValueTask<int> LevenshteinDistanceAsync(string s1, string s2, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(LevenshteinDistance(s1, s2));
    }

    private static int[] ComputeLps(string pattern)
    {
        var lps = new int[pattern.Length];
        var len = 0;
        var i = 1;

        while (i < pattern.Length)
        {
            if (pattern[i] == pattern[len])
            {
                len++;
                lps[i] = len;
                i++;
            }
            else
            {
                if (len != 0)
                    len = lps[len - 1];
                else
                {
                    lps[i] = 0;
                    i++;
                }
            }
        }

        return lps;
    }
}
