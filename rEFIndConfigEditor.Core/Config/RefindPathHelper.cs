namespace rEFIndConfigEditor.Config;

public static class RefindPathHelper
{
    public static string? GetRefindDirectory(string? refindConfPath)
    {
        if (string.IsNullOrWhiteSpace(refindConfPath))
            return null;
        var dir = Path.GetDirectoryName(refindConfPath);
        return string.IsNullOrEmpty(dir) ? null : dir;
    }

    public static string NormalizeSlashes(string path) =>
        path.Replace('\\', '/');

    public static string? ToRefindRelative(string? refindConfPath, string absolutePath)
    {
        var root = GetRefindDirectory(refindConfPath);
        if (root is null)
            return null;

        var fullRoot = Path.GetFullPath(root);
        var fullTarget = Path.GetFullPath(absolutePath);
        var rootWithSep = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        if (!fullTarget.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullTarget, fullRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        var rel = Path.GetRelativePath(fullRoot, fullTarget);
        return NormalizeSlashes(rel);
    }

    public static string? TryExtractThemesRelative(string absolutePath)
    {
        var normalized = NormalizeSlashes(Path.GetFullPath(absolutePath));
        var idx = normalized.IndexOf("/themes/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;
        return normalized[(idx + 1)..];
    }

    public static string? TryExtractIconsRelative(string absolutePath)
    {
        var normalized = NormalizeSlashes(Path.GetFullPath(absolutePath));
        var idx = normalized.LastIndexOf("/icons/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;
        return normalized[(idx + 1)..];
    }

    public static string? TryEspRefindDirectoryRelative(string refindDirectory)
    {
        var normalized = NormalizeSlashes(Path.GetFullPath(refindDirectory));
        const string marker = "/EFI/refind";
        var idx = normalized.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;
        return normalized[(idx + 1)..];
    }

    public static string ResolveForRefind(string? refindConfPath, string absolutePath)
    {
        var relative = ToRefindRelative(refindConfPath, absolutePath);
        if (relative is not null)
            return relative;

        var themes = TryExtractThemesRelative(absolutePath);
        if (themes is not null)
            return themes;

        return NormalizeSlashes(absolutePath);
    }

    public static IReadOnlyList<string> FindConfFiles(string folder, string? refindConfPath)
    {
        if (!Directory.Exists(folder))
            return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.conf", SearchOption.AllDirectories))
        {
            var rel = refindConfPath is not null
                ? ToRefindRelative(refindConfPath, file) ?? TryExtractThemesRelative(file)
                : TryExtractThemesRelative(file) ?? NormalizeSlashes(file);

            if (rel is null || !seen.Add(rel))
                continue;
            results.Add(rel);
        }

        results.Sort(CompareConfPaths);
        return results;
    }

    private static int ThemeConfPriority(string path)
    {
        var name = Path.GetFileName(path);
        return string.Equals(name, "theme.conf", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private static int CompareConfPaths(string a, string b)
    {
        var pa = ThemeConfPriority(a);
        var pb = ThemeConfPriority(b);
        if (pa != pb)
            return pa.CompareTo(pb);
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
