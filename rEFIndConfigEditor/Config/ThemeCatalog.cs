using Newtonsoft.Json;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Storage;

namespace rEFIndConfigEditor.Config;

internal static class ThemeCatalog
{
    private static IReadOnlyList<ThemeCatalogEntry>? _cached;
    private static DateTime _cachedMtime;

    private static string CatalogPath =>
        Path.Combine(AppContext.BaseDirectory, "themes", "themes.json");

    private static string ThemesRoot =>
        Path.Combine(AppContext.BaseDirectory, "themes");

    public static IReadOnlyList<ThemeCatalogEntry> Load()
    {
        if (!File.Exists(CatalogPath))
        {
            _cached = [];
            return _cached;
        }

        var mtime = File.GetLastWriteTimeUtc(CatalogPath);
        if (_cached is not null && mtime == _cachedMtime)
            return _cached;

        try
        {
            SafeFileIO.EnsureWithinSize(CatalogPath, SafeFileIO.MaxJsonBytes);
            var json = File.ReadAllText(CatalogPath);
            _cached = JsonConvert.DeserializeObject<List<ThemeCatalogEntry>>(json) ?? [];
            _cachedMtime = mtime;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"ThemeCatalog.Load failed: {ex}");
            _cached ??= [];
        }
        return _cached;
    }

    public static string? PreviewPath(ThemeCatalogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Preview))
            return null;

        var root = Path.GetFullPath(ThemesRoot);
        var path = Path.GetFullPath(Path.Combine(root, entry.Preview.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
            return null;

        return File.Exists(path) ? path : null;
    }
}
