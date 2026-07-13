namespace Usm.Shared.Utils.FileSystem;

public static class FileSystemHelpers
{
    public static async Task WriteTextSafelyAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }

    public static bool TryDeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }
}
