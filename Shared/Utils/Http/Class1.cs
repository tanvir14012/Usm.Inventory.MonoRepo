namespace Usm.Shared.Utils.Http;

public static class HttpRequestHelpers
{
    public static string ToQueryString(IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        return "?" + string.Join(
            "&",
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }
}
