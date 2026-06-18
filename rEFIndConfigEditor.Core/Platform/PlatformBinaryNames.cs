using rEFIndConfigEditor;

namespace rEFIndConfigEditor.Platform;

public static class PlatformBinaryNames
{
    public static string AppHostFileName =>
        OperatingSystem.IsWindows() ? $"{AppBranding.Slug}.exe" : AppBranding.Slug;

    public static string UpdaterHostFileName =>
        OperatingSystem.IsWindows() ? "updater.exe" : "updater";

    public static bool IsAppHostFileName(string fileName) =>
        string.Equals(fileName, AppHostFileName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileName, AppBranding.Slug, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileName, AppBranding.ExeFileName, StringComparison.OrdinalIgnoreCase);

    public static bool IsUpdaterHostFileName(string fileName) =>
        string.Equals(fileName, UpdaterHostFileName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileName, "updater.exe", StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileName, "updater", StringComparison.OrdinalIgnoreCase);
}
