using rEFIndConfigEditor;

namespace rEFIndConfigEditor.Update;

public static class UpdaterLogger
{
    private static readonly object Gate = new();

    public static void Write(string message)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppBranding.Slug);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "updater.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
            lock (Gate)
                File.AppendAllText(path, line);
        }
        catch
        {
        }
    }
}
