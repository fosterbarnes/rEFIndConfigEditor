using System.Text;

namespace rEFIndConfigEditor.Storage;

internal static class SafeFileIO
{
    public const int MaxConfigBytes = 4 * 1024 * 1024;
    public const int MaxLogBytes = 16 * 1024 * 1024;
    public const int MaxJsonBytes = 256 * 1024;
    public const int MaxThemeZipBytes = 50 * 1024 * 1024;

    public static void EnsureWithinSize(string path, int maxBytes)
    {
        var length = new FileInfo(path).Length;
        if (length > maxBytes)
        {
            throw new InvalidOperationException(
                $"File is too large ({length / (1024 * 1024)} MB). Maximum is {maxBytes / (1024 * 1024)} MB.");
        }
    }

    public static string ReadAllText(string path, int maxBytes, Encoding? encoding = null)
    {
        EnsureWithinSize(path, maxBytes);
        return encoding is null ? File.ReadAllText(path) : File.ReadAllText(path, encoding);
    }
}
