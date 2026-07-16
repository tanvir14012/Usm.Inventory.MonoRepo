using Microsoft.Extensions.Configuration;

namespace Usm.Shared.BuildingBlocks.Bootstrap;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddDotEnvFile(
        this IConfigurationBuilder configurationBuilder,
        string startDirectory,
        string fileName = ".env",
        bool optional = true)
    {
        var filePath = ResolveDotEnvPath(startDirectory, fileName);
        if (filePath is null)
        {
            if (!optional)
            {
                throw new FileNotFoundException($"Could not find '{fileName}' starting from '{startDirectory}'.");
            }

            return configurationBuilder;
        }

        var values = ParseDotEnvFile(filePath);
        return values.Count == 0
            ? configurationBuilder
            : configurationBuilder.AddInMemoryCollection(values);
    }

    private static string? ResolveDotEnvPath(string startDirectory, string fileName)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            var candidatePath = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            current = current.Parent;
        }

        return null;
    }

    private static Dictionary<string, string?> ParseDotEnvFile(string filePath)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = line[(separatorIndex + 1)..].Trim();
            value = UnwrapQuotedValue(value);

            values[key.Replace("__", ":", StringComparison.Ordinal)] = value;
        }

        return values;
    }

    private static string UnwrapQuotedValue(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var first = value[0];
        var last = value[^1];
        if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
        {
            return value[1..^1];
        }

        return value;
    }
}
