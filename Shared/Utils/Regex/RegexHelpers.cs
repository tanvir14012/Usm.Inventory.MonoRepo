using System.Text.RegularExpressions;

namespace Usm.Shared.Utils.Regex;

public static class RegexHelpers
{
    public static bool IsMatch(string input, string pattern, RegexOptions options = RegexOptions.None)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, pattern, options, TimeSpan.FromSeconds(1));
    }

    public static string Replace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.None)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement, options, TimeSpan.FromSeconds(1));
    }
}
