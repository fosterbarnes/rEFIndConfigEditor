using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal static class RefindThemePathHelper
{
    private static readonly HashSet<string> ThemeFileTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "banner", "font", "selection_big", "selection_small"
    };

    public static string FontFileDialogFilter =>
        "PNG font (*.png)|*.png|All files (*.*)|*.*";

    public static bool IsThemeFileToken(string token) => ThemeFileTokens.Contains(token);

    public static bool IsIconsDirToken(string token) =>
        string.Equals(token, "icons_dir", StringComparison.OrdinalIgnoreCase);

    public static string FormatPickedThemeFile(string absoluteFile, string? refindConfPath)
    {
        var full = Path.GetFullPath(absoluteFile);
        var underRefind = RefindPathHelper.ToRefindRelative(refindConfPath, full);
        if (underRefind is not null)
            return underRefind;

        return Path.GetFileName(full);
    }

    public static string FormatPickedIconsDir(string absoluteFolder, string? refindConfPath)
    {
        var full = Path.GetFullPath(absoluteFolder);
        var underRefind = RefindPathHelper.ToRefindRelative(refindConfPath, full);
        if (underRefind is not null)
            return underRefind.TrimEnd('/', '\\');

        return Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public static string? ResolveIconsDirBrowsePath(string? refindConfPath, string? currentValue)
    {
        var refindDir = RefindPathHelper.GetRefindDirectory(refindConfPath);
        if (refindDir is null)
            return null;

        if (string.IsNullOrWhiteSpace(currentValue))
            return refindDir;

        var rel = currentValue.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var candidate = Path.IsPathRooted(rel) ? rel : Path.Combine(refindDir, rel);
        return Directory.Exists(candidate) ? candidate : refindDir;
    }
}
