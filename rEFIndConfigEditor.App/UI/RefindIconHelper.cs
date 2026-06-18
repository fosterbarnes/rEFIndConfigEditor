using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal sealed record RefindIconChoice(
    string Label,
    string ConfigPath,
    string? AbsolutePath,
    bool OnDisk,
    bool CatalogOnly);

internal static class RefindIconHelper
{
    private static readonly string[] IconExtensions = [".png", ".icns", ".bmp", ".jpg", ".jpeg"];

    public static string IconFileDialogFilter =>
        "Icon images|*.png;*.icns;*.bmp;*.jpg;*.jpeg|PNG (*.png)|*.png|ICNS (*.icns)|*.icns|Bitmap (*.bmp)|*.bmp|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|All files (*.*)|*.*";

    public static IReadOnlyList<string> GetSearchDirectories(string? refindConfPath, RefindDocument? document)
    {
        var refindDir = RefindPathHelper.GetRefindDirectory(refindConfPath);
        if (refindDir is null)
            return [];

        var dirs = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string path)
        {
            var full = Path.GetFullPath(path);
            if (!Directory.Exists(full) || !seen.Add(full))
                return;
            dirs.Add(full);
        }

        Add(Path.Combine(refindDir, "icons"));

        var iconsDir = GetActiveIconsDir(document);
        if (!string.IsNullOrWhiteSpace(iconsDir))
        {
            var rel = iconsDir.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            if (!Path.IsPathRooted(rel))
                Add(Path.Combine(refindDir, rel));
            else if (Directory.Exists(rel))
                Add(rel);
        }

        return dirs;
    }

    public static string? GetInitialBrowseDirectory(string? refindConfPath, RefindDocument? document)
    {
        foreach (var dir in GetSearchDirectories(refindConfPath, document))
            return dir;

        return RefindPathHelper.GetRefindDirectory(refindConfPath);
    }

    public static string FormatIconPath(string absoluteFile, string? refindConfPath, RefindDocument? document = null)
    {
        var full = Path.GetFullPath(absoluteFile);
        var fileName = Path.GetFileName(full);
        var refindDir = RefindPathHelper.GetRefindDirectory(refindConfPath);

        var underRefind = RefindPathHelper.ToRefindRelative(refindConfPath, full);
        if (underRefind is not null)
            return QualifyEspRefindIconPath(refindDir, underRefind);

        var fromIcons = RefindPathHelper.TryExtractIconsRelative(full);
        if (fromIcons is not null)
            return QualifyEspRefindIconPath(refindDir, fromIcons);

        return DefaultIconConfigPath(refindConfPath, document, fileName);
    }

    private static string QualifyEspRefindIconPath(string? refindDir, string path)
    {
        var normalized = RefindPathHelper.NormalizeSlashes(path);
        if (refindDir is null
            || !normalized.StartsWith("icons/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        var espPrefix = RefindPathHelper.TryEspRefindDirectoryRelative(refindDir);
        return espPrefix is not null ? $"{espPrefix}/{normalized}" : normalized;
    }

    private static string DefaultIconConfigPath(string? refindConfPath, RefindDocument? document, string fileName)
    {
        var refindDir = RefindPathHelper.GetRefindDirectory(refindConfPath);
        var iconsFolder = GetActiveIconsDir(document)?.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(iconsFolder))
            iconsFolder = "icons";

        if (refindDir is not null)
        {
            var espPrefix = RefindPathHelper.TryEspRefindDirectoryRelative(refindDir);
            if (espPrefix is not null)
                return $"{espPrefix}/{iconsFolder}/{fileName}";
        }

        return $"{iconsFolder}/{fileName}";
    }

    public static IReadOnlyList<RefindIconChoice> BuildChoices(string? refindConfPath, RefindDocument? document)
    {
        var onDisk = ScanOnDisk(refindConfPath, document);
        var onDiskNames = new HashSet<string>(
            onDisk.Select(c => Path.GetFileName(c.ConfigPath)),
            StringComparer.OrdinalIgnoreCase);

        var choices = new List<RefindIconChoice>(onDisk);
        foreach (var entry in RefindIconCatalog.Standard)
        {
            if (onDiskNames.Contains(entry.FileName))
                continue;

            var resolved = ResolveCatalogPath(refindConfPath, document, entry.FileName);
            choices.Add(new RefindIconChoice(
                entry.Label,
                resolved.ConfigPath,
                resolved.AbsolutePath,
                resolved.OnDisk,
                !resolved.OnDisk));
        }

        return choices;
    }

    public static bool CanLoadThumbnail(string? absolutePath)
    {
        if (absolutePath is null)
            return false;
        var ext = Path.GetExtension(absolutePath);
        return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetActiveIconsDir(RefindDocument? document)
    {
        var option = document?.FindGlobal("icons_dir");
        if (option is null || !option.IsActive || option.Values.Count == 0)
            return null;
        return option.Values[0].Trim();
    }

    private static List<RefindIconChoice> ScanOnDisk(string? refindConfPath, RefindDocument? document)
    {
        var results = new List<RefindIconChoice>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in GetSearchDirectories(refindConfPath, document))
        {
            foreach (var ext in IconExtensions)
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*" + ext, SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(file);
                    if (!seen.Add(name))
                        continue;

                    var configPath = FormatIconPath(file, refindConfPath, document);
                    var label = RefindIconCatalog.Standard
                        .FirstOrDefault(e => string.Equals(e.FileName, name, StringComparison.OrdinalIgnoreCase))
                        ?.Label ?? name;
                    results.Add(new RefindIconChoice(label, configPath, file, true, false));
                }
            }
        }

        results.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private static (string ConfigPath, string? AbsolutePath, bool OnDisk) ResolveCatalogPath(
        string? refindConfPath,
        RefindDocument? document,
        string fileName)
    {
        foreach (var dir in GetSearchDirectories(refindConfPath, document))
        {
            var full = Path.Combine(dir, fileName);
            if (!File.Exists(full))
                continue;
            return (FormatIconPath(full, refindConfPath, document), full, true);
        }

        return (DefaultIconConfigPath(refindConfPath, document, fileName), null, false);
    }
}
